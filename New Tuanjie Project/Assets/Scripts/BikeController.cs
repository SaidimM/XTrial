using UnityEngine;

public class BikeController : MonoBehaviour
{
    [Header("Physics")]
    public float gravity = 18f;
    public float mass = 80f;
    public float inertia = 3000f;
    public float wheelBase = 1.28f;
    public float wheelRadius = 0.34f;
    public float restLength = 0.14f;
    public float springK = 9000f;
    public float damperC = 520f;
    public float frictionMu = 1.25f;
    public float engineForce = 2800f;
    public float brakeForce = 2000f;
    [Header("Balance controls")]
    [Tooltip("Maximum rider rotation speed in degrees per second.")]
    [Range(20f, 120f)] public float maxLeanSpeed = 65f;
    [Tooltip("Time for balance input to reach full strength.")]
    [Range(.05f, .5f)] public float leanRampTime = .16f;
    [Tooltip("How quickly rotation settles after releasing balance input.")]
    [Range(1f, 12f)] public float leanReleaseDamping = 7f;
    // Keep the existing serialized field, with a much gentler default acceleration.
    public float leanTorque = 18000f;

    [Header("Crash forgiveness")]
    [Tooltip("Helmet overlap allowed before contact counts toward a crash, in metres.")]
    [Range(.05f, .25f)] public float helmetContactTolerance = .12f;
    [Tooltip("Time allowed to recover from helmet contact, in seconds.")]
    [Range(.1f, .6f)] public float crashContactDelay = .28f;
    [Tooltip("Normal impact speed required for an immediate helmet crash, in m/s.")]
    public float hardCrashSpeed = 7f;

    float headContactTime, bodyContactTime;
    Vector2 velocity;
    float angularVel;
    float leanInput;
    float bikeAngle;
    float rearWheelAngle;
    float frontWheelAngle;
    bool crashed;
    bool finished;

    [HideInInspector] public bool touchGas, touchBrake, touchLeanBack, touchLeanForward;

    TerrainGenerator terrain;
    GameManager game;
    Transform rearWheel, frontWheel, body, rider;

    void Start()
    {
        terrain = FindObjectOfType<TerrainGenerator>();
        game = FindObjectOfType<GameManager>();
        CreateVisuals();
        ResetBike();
    }

    void CreateVisuals()
    {
        body = new GameObject("Trials motorcycle").transform;
        body.SetParent(transform, false);
        Color ink = TrialArt.Ink;
        Color metal = TrialArt.Hex(0xA9B7AE);
        TrialArt.Line(body, "Swingarm", metal, .075f, 12,
            new Vector2(-.64f, -.16f), new Vector2(.06f, -.02f), new Vector2(.5f, .4f));
        TrialArt.Line(body, "Frame", ink, .075f, 13,
            new Vector2(-.64f, -.16f), new Vector2(-.22f, .32f), new Vector2(.28f, .3f),
            new Vector2(.02f, -.13f), new Vector2(-.64f, -.16f));
        TrialArt.Disc(body, "Engine", new Vector2(.02f, .02f), .18f, ink, 14);
        for (int i = 0; i < 4; i++)
            TrialArt.Line(body, "Cooling fin", metal, .018f, 15,
                new Vector2(-.09f, -.06f + i * .05f), new Vector2(.12f, -.06f + i * .05f));
        TrialArt.Line(body, "Fork", metal, .065f, 15, new Vector2(.64f, -.16f), new Vector2(.42f, .55f));
        TrialArt.Line(body, "Fork lower", ink, .085f, 16, new Vector2(.64f, -.16f), new Vector2(.54f, .14f));
        TrialArt.Line(body, "Handlebar", ink, .05f, 16,
            new Vector2(.42f, .5f), new Vector2(.37f, .6f), new Vector2(.56f, .62f));
        TrialArt.Polygon(body, "Tank", TrialArt.Accent, 16,
            new Vector2(-.35f, .24f), new Vector2(-.19f, .43f), new Vector2(.22f, .4f), new Vector2(.31f, .23f));
        TrialArt.Line(body, "Seat", ink, .08f, 17, new Vector2(-.52f, .34f), new Vector2(-.12f, .35f));
        TrialArt.Line(body, "Rear fender", TrialArt.Accent, .055f, 15, new Vector2(-.85f, .21f), new Vector2(-.44f, .29f));
        TrialArt.Line(body, "Front fender", TrialArt.Accent, .055f, 17, new Vector2(.36f, .22f), new Vector2(.85f, .18f));
        TrialArt.Polygon(body, "Number plate", TrialArt.Cream, 17,
            new Vector2(.32f, .39f), new Vector2(.48f, .43f), new Vector2(.55f, .26f), new Vector2(.4f, .24f));
        rearWheel = CreateWheel("Rear wheel");
        frontWheel = CreateWheel("Front wheel");

        rider = new GameObject("Rider").transform;
        rider.SetParent(body, false);
        TrialArt.Line(rider, "Far arm", TrialArt.Hex(0x566B67), .09f, 18,
            new Vector2(-.05f, .91f), new Vector2(.22f, .66f), new Vector2(.5f, .6f));
        TrialArt.Line(rider, "Boot and leg", ink, .13f, 19,
            new Vector2(-.35f, .55f), new Vector2(.05f, .37f), new Vector2(-.12f, .03f), new Vector2(.05f, .03f));
        TrialArt.Line(rider, "Trousers", TrialArt.Hex(0x54716C), .15f, 20,
            new Vector2(-.36f, .58f), new Vector2(.04f, .38f), new Vector2(-.09f, .13f));
        TrialArt.Polygon(rider, "Jacket", TrialArt.Cream, 21,
            new Vector2(-.44f, .55f), new Vector2(-.33f, .86f), new Vector2(-.07f, 1.02f),
            new Vector2(.07f, .85f), new Vector2(-.2f, .5f));
        TrialArt.Line(rider, "Sleeve", TrialArt.Accent, .105f, 22,
            new Vector2(-.09f, .91f), new Vector2(.16f, .71f), new Vector2(.44f, .61f));
        TrialArt.Disc(rider, "Glove", new Vector2(.46f, .61f), .065f, ink, 23);
        TrialArt.Disc(rider, "Helmet", new Vector2(.01f, 1.13f), .205f, TrialArt.Cream, 23);
        TrialArt.Polygon(rider, "Helmet chin", TrialArt.Cream, 23,
            new Vector2(.02f, 1.13f), new Vector2(.25f, 1.09f), new Vector2(.2f, .96f), new Vector2(.02f, .98f));
        TrialArt.Line(rider, "Visor", ink, .075f, 24, new Vector2(.05f, 1.16f), new Vector2(.22f, 1.13f));
        TrialArt.Line(rider, "Helmet stripe", TrialArt.Accent, .045f, 24,
            new Vector2(-.15f, 1.21f), new Vector2(-.03f, 1.26f), new Vector2(.12f, 1.24f));
    }

    Transform CreateWheel(string name)
    {
        var wheel = new GameObject(name).transform;
        wheel.SetParent(transform, false);
        TrialArt.Disc(wheel, "Tyre", Vector2.zero, wheelRadius, TrialArt.Ink, 10);
        TrialArt.Disc(wheel, "Rim", Vector2.zero, wheelRadius * .74f, TrialArt.Hex(0xB7BAB0), 11);
        TrialArt.Disc(wheel, "Interior", Vector2.zero, wheelRadius * .63f, TrialArt.Hex(0x3C504C), 12);
        for (int i = 0; i < 10; i++)
        {
            float angle = i * Mathf.PI * 2 / 10;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            TrialArt.Line(wheel, "Spoke", TrialArt.Hex(0xC3C8BA), .012f, 13, direction * .04f, direction * wheelRadius * .65f);
        }
        TrialArt.Disc(wheel, "Hub", Vector2.zero, .065f, TrialArt.Sand, 14);
        return wheel;
    }

    public void ResetBike()
    {
        float startX = TrialLevels.StartX;
        transform.position = new Vector3(startX, terrain.GetHeight(startX) + wheelRadius + restLength + 0.55f, 0);
        velocity = Vector2.zero;
        angularVel = 0f;
        leanInput = 0f;
        bikeAngle = 0f;
        rearWheelAngle = 0f;
        frontWheelAngle = 0f;
        headContactTime = bodyContactTime = 0f;
        crashed = false;
        finished = false;
        touchGas = touchBrake = touchLeanBack = touchLeanForward = false;
        UpdateVisuals();
        if (Camera.main != null) Camera.main.transform.position = new Vector3(startX + 3.2f, 3f, -10);
    }

    void Update()
    {
        if (game != null && game.IsSelectingLevel) return;
        if (Input.GetKeyDown(KeyCode.R))
        {
            FindObjectOfType<GameManager>()?.Restart();
            return;
        }
        if (crashed || finished || Time.timeScale == 0) return;

        bool inputGas = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W) || touchGas;
        bool inputBrake = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S) || touchBrake;
        bool inputLeanBack = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A) || touchLeanBack;
        bool inputLeanForward = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D) || touchLeanForward;

        float dt = 1f / 120f;
        int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Min(Time.deltaTime, .05f) / dt));
        float stepDt = Mathf.Min(Time.deltaTime, .05f) / steps;
        for (int i = 0; i < steps && !crashed; i++)
            PhysicsStep(stepDt, inputGas, inputBrake, inputLeanBack, inputLeanForward);

        UpdateVisuals();
        UpdateCamera();

        if (transform.position.x >= TerrainGenerator.FinishX && !finished && !crashed)
        {
            finished = true;
            FindObjectOfType<GameManager>()?.CompleteRun();
        }
    }

    void PhysicsStep(float dt, bool gas, bool brake, bool leanBack, bool leanForward)
    {
        velocity.y -= gravity * dt;

        float driveR = gas ? 1f : (brake ? -1f : 0f);
        float vtr = WheelForce(new Vector2(-wheelBase * 0.5f, -0.02f), driveR, true, dt);

        float driveF = brake ? -1f : 0f;
        float vtf = WheelForce(new Vector2(wheelBase * 0.5f, -0.02f), driveF, false, dt);

        float lean = 0f;
        if (leanForward) lean -= 1f;
        if (leanBack) lean += 1f;
        leanInput = Mathf.MoveTowards(leanInput, lean, dt / Mathf.Max(.05f, leanRampTime));
        if (lean != 0f)
        {
            // Aim for a bounded rotation rate instead of accelerating into a spin.
            float targetRate = leanInput * maxLeanSpeed * Mathf.Deg2Rad;
            // The cap also protects older scenes that serialized the original torque.
            float acceleration = Mathf.Clamp(leanTorque / Mathf.Max(1f, inertia), 1f, 6f);
            angularVel = Mathf.MoveTowards(angularVel, targetRate, acceleration * dt);
        }
        else
        {
            // Releasing a key immediately starts settling rotation, even during input ramp-down.
            angularVel *= Mathf.Exp(-leanReleaseDamping * dt);
        }

        transform.position += new Vector3(velocity.x, velocity.y, 0) * dt;
        bikeAngle += angularVel * dt;

        if (Mathf.Abs(vtr) > 0.001f) rearWheelAngle += vtr / wheelRadius * dt;
        if (Mathf.Abs(vtf) > 0.001f) frontWheelAngle += vtf / wheelRadius * dt;

        CheckCrash(dt);
    }

    void CheckCrash(float dt)
    {
        // Use the visible helmet position, including the rider's balance pose.
        float riderAngle = bikeAngle + leanInput * 9f * Mathf.Deg2Rad;
        Vector3 headOffset = new Vector3(
            Mathf.Cos(riderAngle) * .01f - Mathf.Sin(riderAngle) * 1.13f,
            Mathf.Sin(riderAngle) * .01f + Mathf.Cos(riderAngle) * 1.13f, 0);
        Vector3 head = transform.position + headOffset;
        float slope = terrain.GetSlope(head.x);
        Vector2 normal = new Vector2(-slope, 1f).normalized;
        // Measure perpendicular to the slope so steep ramps don't exaggerate overlap.
        float headPenetration = .2f - (head.y - terrain.GetHeight(head.x)) * normal.y;
        Vector2 headVelocity = velocity + new Vector2(-angularVel * headOffset.y, angularVel * headOffset.x);
        float impactSpeed = Mathf.Max(0, -Vector2.Dot(headVelocity, normal));
        float bodyPenetration = terrain.GetHeight(transform.position.x) - transform.position.y;

        if (HasFatalContact(headPenetration, impactSpeed, bodyPenetration, dt,
            helmetContactTolerance, crashContactDelay, hardCrashSpeed, ref headContactTime, ref bodyContactTime))
        {
            crashed = true;
            FindObjectOfType<GameManager>()?.ShowMessage("BACK IN THE SADDLE");
        }
    }

    // Contact must persist; handlebars and wheels are never fatal contact points.
    internal static bool HasFatalContact(float headPenetration, float impactSpeed,
        float bodyPenetration, float dt, float tolerance, float delay, float hardSpeed,
        ref float headTime, ref float bodyTime)
    {
        headTime = headPenetration > tolerance ? headTime + dt : 0f;
        bodyTime = bodyPenetration > .25f ? bodyTime + dt : 0f;
        bool hardHeadImpact = headPenetration > tolerance && impactSpeed >= hardSpeed;
        return hardHeadImpact || headTime >= delay || bodyTime >= .6f || bodyPenetration > 3f;
    }

    float WheelForce(Vector2 offset, float drive, bool isRear, float dt)
    {
        float s = Mathf.Sin(bikeAngle);
        float c = Mathf.Cos(bikeAngle);

        Vector3 anchor = transform.position + new Vector3(c * offset.x - s * offset.y, s * offset.x + c * offset.y, 0);
        Vector3 wheelC = anchor + new Vector3(0, -restLength, 0);

        float groundY = terrain.GetHeight(wheelC.x);
        float penetration = groundY - (wheelC.y - wheelRadius);
        if (penetration <= 0) return 0f;

        float rx = anchor.x - transform.position.x;
        float ry = anchor.y - transform.position.y;
        float pvx = velocity.x - angularVel * ry;
        float pvy = velocity.y + angularVel * rx;

        float slope = terrain.GetSlope(wheelC.x);
        float tl = Mathf.Sqrt(1f + slope * slope);
        float nx = -slope / tl;
        float ny = 1f / tl;
        float tx = 1f / tl;
        float ty = slope / tl;

        float vn = pvx * nx + pvy * ny;
        float N = springK * penetration - damperC * vn;
        if (N < 0) N = 0;

        float vt = pvx * tx + pvy * ty;
        float Ft = -vt * 90f;
        if (drive > 0 && isRear) Ft += engineForce * drive;
        if (drive < 0) Ft += brakeForce * drive * (isRear ? 0.7f : 1f);

        float maxF = frictionMu * N;
        Ft = Mathf.Clamp(Ft, -maxF, maxF);

        float cx = wheelC.x - transform.position.x;
        float cy = groundY - transform.position.y;
        float Fx = nx * N + tx * Ft;
        float Fy = ny * N + ty * Ft;

        velocity.x += Fx / mass * dt;
        velocity.y += Fy / mass * dt;
        angularVel += (cx * Fy - cy * Fx) / inertia * dt;

        return vt;
    }

    void UpdateCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        float viewSize = Mathf.Max(4.8f, 7f / Mathf.Max(cam.aspect, .5f));
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, viewSize, 1 - Mathf.Exp(-3 * Time.deltaTime));
        float lookAhead = 2.8f + Mathf.Clamp(velocity.x * .12f, 0, 2);
        Vector3 target = new Vector3(transform.position.x + lookAhead, Mathf.Max(3, transform.position.y + 1.4f), -10);
        cam.transform.position = Vector3.Lerp(cam.transform.position, target, 1 - Mathf.Exp(-4 * Time.deltaTime));
    }

    void UpdateVisuals()
    {
        float s = Mathf.Sin(bikeAngle);
        float c = Mathf.Cos(bikeAngle);

        Vector3 rOff = new Vector3(-wheelBase * 0.5f, -0.02f, 0);
        Vector3 fOff = new Vector3(wheelBase * 0.5f, -0.02f, 0);
        Vector3 rAnchor = transform.position + new Vector3(c * rOff.x - s * rOff.y, s * rOff.x + c * rOff.y, 0);
        Vector3 fAnchor = transform.position + new Vector3(c * fOff.x - s * fOff.y, s * fOff.x + c * fOff.y, 0);

        rearWheel.position = rAnchor + new Vector3(0, -restLength, 0);
        frontWheel.position = fAnchor + new Vector3(0, -restLength, 0);
        rearWheel.rotation = Quaternion.Euler(0, 0, rearWheelAngle * Mathf.Rad2Deg);
        frontWheel.rotation = Quaternion.Euler(0, 0, frontWheelAngle * Mathf.Rad2Deg);

        body.position = transform.position;
        body.rotation = Quaternion.Euler(0, 0, bikeAngle * Mathf.Rad2Deg);
        rider.localRotation = Quaternion.Euler(0, 0, leanInput * 9);
    }

    public float GetSpeed() => velocity.magnitude * 3.6f;
    public float GetDistance() => transform.position.x;
    public bool IsCrashed() => crashed;
    public bool IsFinished() => finished;
}
