using UnityEngine;

public class PlayerMover : MonoBehaviour
{
    public Vector3 velocity { get; private set; } = Vector3.zero;
    private float acceleration;

    public Vector3 CalcVelocity(Vector3 target)
    {
        velocity = Vector3.MoveTowards(velocity, target, acceleration * Time.deltaTime);
        return velocity;
    }

    public void SetAcceleration(float newAcceleration)
    {
        acceleration = newAcceleration;
    }

    public void Stop()
    {
        acceleration = 0.0f;
        velocity = Vector3.zero;
    }

    public void SetVelocity(Vector3 newVelocity)
    {
        newVelocity.y = 0.0f;
        velocity = newVelocity;
    }

    public void AddVelocity(Vector3 addVelocity)
    {
        addVelocity.y = 0.0f;
        velocity += addVelocity;
    }
}
