using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using crass;

[RequireComponent(typeof(Collider2D))]
public class Platformer2D : MonoBehaviour
{
    [Serializable]
    public struct BasicMovementProfile
    {
        // acceleration is how fast your speed changes while a button is held down, and deceleration is how quickly your speed returns back to zero. each are in units per second
        public float MaxSpeed, Acceleration, Deceleration;
    }

    [Header("Stats")]
    public float Gravity;
    public BasicMovementProfile GroundProfile, AirProfile;

    // burst: initial instantaneous speed value when jumping
    // cut: when you release the jump button, your vertical speed is set to this if it's currently higher. essentially an air control value
    public float JumpSpeedBurst, JumpSpeedCut;
    public float EarlyJumpPressTime; // you can input jump this many seconds before landing and it will still count

    public float HalfHeight;
    public Vector2 GroundCheckBoxDimensions; // should typically be set to x = player width plus walking over platform distance, y = vertical fudge
    public LayerMask GroundLayers;

    [Header("Controls")]
    public string MoveAxis;
    public string JumpButton;

    [Header("Sound")]
    public float WallBumpRepeatTime;
    public TransitionableFloat JumpQuickFader;

    [Header("References")]
    public Rigidbody2D Rigidbody;
    public AudioSource JumpSource, LandSource, WallBumpSource, CeilingBumpSource;

    bool grounded => Physics2D.BoxCast(transform.position, GroundCheckBoxDimensions, 0, Vector2.down, HalfHeight, GroundLayers);

    float moveInput;
    bool jumpInputPressed, jumpInputHeld;

    bool jumping;

    float earlyJumpPressTimer;

    float wallBumpRepeatTimer;

    float initialJumpSourceVolume;

    void Start ()
    {
        initialJumpSourceVolume = JumpSource.volume;

        JumpQuickFader.AttachMonoBehaviour(this);
        JumpQuickFader.Value = initialJumpSourceVolume;
    }

    void Update ()
    {
        // call this in Update because tracking in FixedUpdate leads to dropped input
        trackInput();

        // so that the response is instantaneous if the player walks into a wall, stops the button, and then presses the button again 
        wallBumpRepeatTimer -= Time.deltaTime;

        JumpSource.volume = JumpQuickFader.Value;
    }

    void FixedUpdate ()
    {
        Vector2 newVelocity = Rigidbody.velocity;

        newVelocity.y -= Gravity * Time.deltaTime;

        // JUMP STATE

        if (grounded && !jumping && earlyJumpPressTimer > 0)
        {
            // start of jump
            newVelocity.y = JumpSpeedBurst;
            jumping = true;

            playJumpSource();
        }
        else if (grounded && jumping && Rigidbody.velocity.y <= 0)
        {
            // landing
            jumping = false;
        }
        else if (jumping && !jumpInputHeld && Rigidbody.velocity.y > JumpSpeedCut)
        {
            // letting go of jump
            newVelocity.y = JumpSpeedCut;

            fadeOutJumpSource();
        }

        // HORIZONTAL STATE

        var profile = grounded ? GroundProfile : AirProfile;

        if (moveInput == 0)
        {
            Vector2 deceleration = Mathf.Sign(Rigidbody.velocity.x) * Vector2.right * profile.Deceleration * Time.deltaTime;
            deceleration = Vector2.ClampMagnitude(deceleration, Mathf.Abs(Rigidbody.velocity.x));
            newVelocity -= deceleration;
        }
        else
        {
            float acceleration = profile.Acceleration;

            if (Rigidbody.velocity.x != 0 && ternarySign(moveInput) != ternarySign(Rigidbody.velocity.x))
            {
                // if we're switching directions, and the deceleration is faster, use the deceleration
                acceleration = Mathf.Max(profile.Acceleration, profile.Deceleration);
            }

            newVelocity.x += moveInput * profile.Acceleration * Time.deltaTime;
            newVelocity.x = Mathf.Clamp(newVelocity.x, -profile.MaxSpeed, profile.MaxSpeed);
        }

        // FINAL STATE

        Rigidbody.velocity = newVelocity;
    }

    void OnCollisionEnter2D (Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts.DistinctBy(c => c.collider))
        {
            // assumes that all collisions are axis aligned
            if (contact.normal == Vector2.up)
            {
                LandSource.Play();
            }
            else if (contact.normal == Vector2.down)
            {
                CeilingBumpSource.Play();
                fadeOutJumpSource();
            }
            else
            {
                WallBumpSource.Play();
                wallBumpRepeatTimer = WallBumpRepeatTime;
            }
        }
    }

    void OnCollisionStay2D (Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts.DistinctBy(c => c.collider))
        {
            if (contact.normal != moveInput * Vector2.left) continue;

            if (wallBumpRepeatTimer < 0)
            {
                wallBumpRepeatTimer = WallBumpRepeatTime;
                WallBumpSource.Play();
            }
        }
    }

    void trackInput ()
    {
        moveInput = Input.GetAxis(MoveAxis);
        jumpInputPressed = Input.GetButtonDown(JumpButton);
        jumpInputHeld = Input.GetButton(JumpButton);

        if (jumpInputPressed) earlyJumpPressTimer = EarlyJumpPressTime;
        else earlyJumpPressTimer -= Time.deltaTime;
    }

    void playJumpSource ()
    {
        JumpQuickFader.Value = initialJumpSourceVolume;
        JumpSource.volume = initialJumpSourceVolume;
        JumpSource.Play();
    }

    void fadeOutJumpSource ()
    {
        initialJumpSourceVolume = JumpSource.volume;
        JumpQuickFader.Value = initialJumpSourceVolume;
        JumpQuickFader.StartTransitionTo(0);
    }

    // explicitly classify 0 as different from positive/negative, since mathf.sign classifies 0 as positive
    float ternarySign (float x)
    {
        if (x > 0) return 1;
        else if (x < 0) return -1;
        else return 0;
    }
}

// list of non-code things done to make this work:
    // not gonna list all of the rigidbody/collider changes so make sure to check those
    // 0-friction physics material
    // movement axis set to 1000/1000 gravity/sensitivity
    // create a layer for the player, to be ignored by grounded check
