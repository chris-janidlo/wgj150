using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    [Header("References")]
    public Rigidbody2D Rigidbody;

    bool grounded => Physics2D.BoxCast(transform.position, GroundCheckBoxDimensions, 0, Vector2.down, HalfHeight, GroundLayers);

    float moveInput;
    bool jumpInputPressed, jumpInputHeld;

    bool jumping;

    float earlyJumpPressTimer;

    void Update ()
    {
        // call this in Update because tracking in FixedUpdate leads to dropped input
        trackInput();
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

    void trackInput ()
    {
        moveInput = Input.GetAxis(MoveAxis);
        jumpInputPressed = Input.GetButtonDown(JumpButton);
        jumpInputHeld = Input.GetButton(JumpButton);

        if (jumpInputPressed) earlyJumpPressTimer = EarlyJumpPressTime;
        else earlyJumpPressTimer -= Time.deltaTime;
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
