using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using crass;

[RequireComponent(typeof(Collider2D))]
public class MovingPlatform : MonoBehaviour
{
    public Vector2 OffsetFromStart;
    public TransitionableVector2 DistanceTransition;

    Vector2 startPosition;

    bool wasTravelingToOffset;

    void Start ()
    {
        startPosition = transform.position;
        DistanceTransition.AttachMonoBehaviour(this);
    }

    void Update ()
    {
        if (!DistanceTransition.Transitioning)
        {
            Vector2 target = wasTravelingToOffset ? startPosition : startPosition + OffsetFromStart;
            wasTravelingToOffset = !wasTravelingToOffset;

            DistanceTransition.FlashFromTo(transform.position, target);
        }

        transform.position = DistanceTransition.Value;
    }

    void OnCollisionEnter2D (Collision2D collision)
    {
        if (collision.gameObject.GetComponent<Platformer2D>() == null) return;

        if (collision.GetContact(0).normal != Vector2.down) return;

        collision.collider.transform.parent = transform;
    }

    void OnCollisionExit2D (Collision2D collision)
    {
        if (collision.gameObject.GetComponent<Platformer2D>() == null) return;

        collision.collider.transform.parent = null;
    }
}
