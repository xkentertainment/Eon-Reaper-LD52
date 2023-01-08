using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

//As shit as this controller is, it may be the best platforming character controller I've ever devised. And I hate it
public class Player : MonoBehaviour
{
    CharacterController controller;
    [SerializeField]
    float speed;
    [SerializeField]
    float weight;
    public bool Dead => !alive;

    bool JumpDown => Input.GetButtonDown("Jump");
    bool JumpHeld => Input.GetButton("Jump");

    bool WalkHeld => Input.GetButton("Fire1");

    bool CamViewHeld => Input.GetButton("Fire2");

    public bool CamViewMode => CamViewHeld;

    public static float TimeFlow { get; private set; } = 1f;

    public static Player Instance { get; private set; }

    [SerializeField]
    Animator animator;
    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        controller = GetComponent<CharacterController>();
        StartCoroutine(TimeFlowRoutine());
        StartCoroutine(CheckOneWays());
        StartCoroutine(OrientModel());
        alive = true;
    }

    Vector2 input;
    Vector2 rawInput;

    Vector3 velocity;

    [SerializeField]
    float maxSpeed;

    int collected;
    public int Collected => collected;
    bool lastJumpState;
    float lastJumpInputChange;

    const float minimumJumpTime = .2f;

    readonly int wallJumpAnimation = Animator.StringToHash("WallJump");

    [SerializeField]
    float animationMax;
    readonly int movementAnimProp = Animator.StringToHash("Movement");

    bool alive;
    // Update is called once per frame <- The line below, is not in fact, the Update method. I don't use update, too janky
    void FixedUpdate()
    {
        if (LevelManager.Instance && LevelManager.Instance.IsComplete)
            return;

        if (!alive)
            return;

        input = new(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        rawInput = new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        bool grounded = Grounded();
        bool jumpInput = rawInput.y > 0 || JumpDown || JumpHeld;

        if (jumpInput != lastJumpState)
        {
            lastJumpInputChange = Time.time;
        }
        if (!CamViewHeld)
            if (!jumping && (grounded || Koyote()) && jumpInput)
            {
                lastJumpInputChange = -1f;
                Jump();
            }

        if (!grounded && jumpInput && Time.time.AbsoluteDifference(lastJumpInputChange) < minimumJumpTime)
        {
            lastJumpInputChange = -1f;
            if (velocity.x > 0)
                TryWallJump(Vector3.right);
            else
                TryWallJump(Vector3.left);

        }

        //Add input to velocity (acceleration)
        velocity.x += (grounded ? 10f : 4f) * (input.x * Time.fixedDeltaTime) * (WalkHeld ? .25f : 1f);

        //Make sure it doesn't get too crazy although drag eases it out
        velocity.x = Mathf.Clamp(velocity.x, -maxSpeed, maxSpeed);

        //Drag
        velocity = Vector3.Lerp(velocity, Vector3.zero, (grounded ? 6f : 4f) * Time.fixedDeltaTime);

        if (!CamViewHeld)
            animator.SetFloat(movementAnimProp, Mathf.Clamp01(Mathf.Abs(velocity.x) / animationMax));
        else
            animator.SetFloat(movementAnimProp, 0f);


        if (!CamViewHeld)
        {
            controller.Move(Time.deltaTime * speed * velocity);
        }

        CollisionFlags coll = controller.Move(weight * Time.deltaTime * jumpFactor * Vector3.down);

        if (jumping && coll.HasFlag(CollisionFlags.Above) && jumpFactor < 0f)
        {
            CutToMovementAnimation();
            JumpCancel();
        }
        if (jumping && coll.HasFlag(CollisionFlags.Below))
        {
            CutToMovementAnimation();
            StopJump();
        }
        if (inWallJump && coll.HasFlag(CollisionFlags.Sides))
        {
            JumpCancel();
            CutToMovementAnimation();
            StopWallJump();
        }

        if (transform.position.z != 0f)
        {
            transform.position = new(transform.position.x, transform.position.y, 0f);
        }
        if (transform.position.y < -7f)
        {
            if (LevelManager.Instance)
            {
                LevelManager.Instance.GameOver();
            }
        }

        lastJumpState = jumpInput;
    }
    [SerializeField]
    AudioSource source;
    void PlaySFX(AudioClip sfx)
    {
        source.pitch = 1f + Random.Range(-.1f, .1f);
        source.PlayOneShot(sfx);
    }
    readonly int movementAnim = Animator.StringToHash("Movement");
    void CutToMovementAnimation(float fade = .1f)
    {
        PlayAnimation(movementAnim, fade);
    }
    void PlayAnimation(int name, float fade = .1f)
    {
        animator.CrossFade(name, fade, -1, 0f);
    }
    [SerializeField]
    Transform model;
    IEnumerator OrientModel()
    {
        while (true)
        {
            Vector3 dir = velocity.x < 0 ? Vector3.right : Vector3.left;

            model.transform.rotation = Quaternion.Lerp(model.transform.rotation, Quaternion.LookRotation(dir, Vector3.up), 10f * Time.deltaTime);

            yield return new WaitForFixedUpdate();
        }
    }
    public Bounds Bounds => controller.bounds;
    void JumpCancel()
    {
        jumpFactor = 0f;
    }
    [SerializeField]
    LayerMask wallJumpable;

    [SerializeField]
    float wallJumpSpeed;
    const float wallJumpMin = 1f;
    //This function is the best solution to quick wall jumps I can think of
    //I just pray some 10000IQ player doesn't exploit this
    //We'll see
    bool TryWallJump(Vector3 dir)
    {
        //Possibly some of the worst code I've written in years
        bool top = Physics.Raycast(transform.position + (Vector3.up * .5f), dir, wallJumpMin, wallJumpable, QueryTriggerInteraction.Ignore);
        bool middle = Physics.Raycast(transform.position, dir, wallJumpMin, wallJumpable, QueryTriggerInteraction.Ignore);
        bool bottom = Physics.Raycast(transform.position + (Vector3.up * -.5f), dir, wallJumpMin, wallJumpable, QueryTriggerInteraction.Ignore);

        Debug.Log($"T{top},M{middle},B{bottom}");

        bool success = (top && middle) || (bottom && middle);

        if (success)
        {
            PlayAnimation(wallJumpAnimation, .05f);
            StopWallJump();
            wallJumpRoutine = StartCoroutine(WallJumpRoutine(dir));
        }

        return success;
    }
    bool inWallJump = false;
    Coroutine wallJumpRoutine;
    const float wallJumpSpeedDiv = .125f;
    const float wallJumpInputInfluence = .5f;
    [SerializeField]
    AudioClip wallJumpSFX;
    IEnumerator WallJumpRoutine(Vector3 dir)
    {
        PlaySFX(wallJumpSFX);
        inWallJump = true;
        velocity = Vector3.zero;
        for (int i = 0; i < 1f / wallJumpSpeedDiv; i++)
        {
            velocity += wallJumpSpeed * wallJumpSpeedDiv * (((Vector3.up * 1.25f) + -dir) + ((Vector3)rawInput * wallJumpInputInfluence));
            yield return new WaitForFixedUpdate();
        }
        inWallJump = false;
    }

    void StopWallJump()
    {
        if (wallJumpRoutine != null)
            StopCoroutine(wallJumpRoutine);

        inWallJump = false;
    }
    const float minMagnitudeChange = .01f;
    IEnumerator TimeFlowRoutine()
    {
        Vector3 lastPosition = transform.position;
        while (true)
        {
            TimeFlow = Mathf.MoveTowards(TimeFlow, (lastPosition.MagnitudeDifference(transform.position) > minMagnitudeChange ? 1f : 0f), 5f * Time.deltaTime) * (WalkHeld ? .75f : 1f);

            lastPosition = transform.position;
            yield return new WaitForFixedUpdate();
        }
    }
    float jumpFactor = 1f;
    bool jumping;
    Coroutine jumpRoutine;
    readonly int jumpAnimation = Animator.StringToHash("Jump");
    void Jump()
    {
        PlayAnimation(jumpAnimation, .01f);
        StopJump();
        jumping = true;
        jumpRoutine = StartCoroutine(JumpRoutine());
        PlaySFX(jumpSFX);
    }
    [SerializeField]
    AudioClip jumpSFX;
    void StopJump()
    {
        if (jumpRoutine != null)
            StopCoroutine(jumpRoutine);

        jumpFactor = 1f;
        jumping = false;
    }
    [SerializeField]
    float jumpSpeed;

    const float jumpSpeedDiv = .2f;

    bool PlayerHoldingJump => JumpHeld || rawInput.y > 0;
    IEnumerator JumpRoutine()
    {
        jumpFactor = -jumpSpeed * jumpSpeedDiv;
        yield return null;
        yield return null;
        while (PlayerHoldingJump && jumpFactor != -jumpSpeed && jumpFactor != 0f)
        {
            jumpFactor -= jumpSpeed * jumpSpeedDiv;
            jumpFactor = Mathf.Clamp(jumpFactor, -jumpSpeed, jumpSpeed);
            yield return new WaitForFixedUpdate();
        }

        while (jumpFactor.AbsoluteDifference(1f) > .1f)
        {
            jumpFactor = Mathf.Lerp(jumpFactor, 1f, 3f * Time.deltaTime);
            yield return new WaitForFixedUpdate();
        }

        jumpFactor = 1f;
        jumping = false;
    }
    //Dumb Unity function not working
    //private void OnCollisionEnter(Collision collision)
    //{
    //    if (jumping && collision.GetContact(0).point.y < transform.position.y)
    //    {
    //        StopJump();
    //    }
    //}
    [SerializeField]
    LayerMask groundLayers;
    const float groundDistance = 1.1f;
    bool Grounded()
    {

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundDistance, groundLayers, QueryTriggerInteraction.Ignore))
            return true;


        if (Physics.Raycast(transform.position + Vector3.left, Vector3.down, out hit, groundDistance, groundLayers, QueryTriggerInteraction.Ignore))
            return true;


        if (Physics.Raycast(transform.position + Vector3.right, Vector3.down, out hit, groundDistance, groundLayers, QueryTriggerInteraction.Ignore))
            return true;


        return false;
    }
    bool Koyote()
    {
        if (Physics.Raycast(transform.position + new Vector3(-input.x * 1.25f, 0, 0), Vector3.down, out RaycastHit hit, groundDistance, groundLayers, QueryTriggerInteraction.Ignore))
        {
            Debug.Log("YESS");
            Debug.Log(hit.point);
            return true;
        }

        return false;
    }
    [SerializeField]
    AudioClip winSFX;
    [SerializeField]
    AudioClip collectSFX;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Collectible"))
        {
            if (other.GetComponent<BlackHole>().Collect())
            {
                PlaySFX(collectSFX);
                collected++;
            }
            return;
        }

        if (other.CompareTag("Door"))
        {
            if(other.GetComponent<Universe>().Enter(collected))
            {
                PlaySFX(winSFX);
            }
            return;
        }
        if (other.CompareTag("Spikes"))
        {
            StopAllCoroutines();
            LevelManager.Instance.GameOver();
            return;
        }
    }
    [SerializeField]
    LayerMask oneWayLayer;
    const float oneWayDistance = 6f;
    IEnumerator CheckOneWays()
    {
        Vector3 lastPos = transform.position;
        while (true)
        {
            while(lastPos == transform.position)
            {
                yield return new WaitForFixedUpdate();
            }
            Vector3 dir = (transform.position - lastPos).normalized;
            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, oneWayDistance, oneWayLayer, QueryTriggerInteraction.Collide))
            {
                hit.collider.gameObject.GetComponent<OneWayWall>().TryPass(dir);
            }
            lastPos = transform.position;
            yield return new WaitForFixedUpdate();
        }
    }
    [SerializeField]
    AudioClip dieSFX;
    public void Break(bool good = false)
    {
        PlaySFX(dieSFX);
        StopAllCoroutines();
        alive = false;
        StartCoroutine(BreakRoutine(good));
    }
    [SerializeField]
    VisualEffect breakParticles;
    [SerializeField]
    VisualEffect goodBreakParticles;
    IEnumerator BreakRoutine(bool good)
    {
        this.PlayVFX(good ? goodBreakParticles : breakParticles);
        yield return new WaitForSeconds(.3f);
        model.gameObject.SetActive(false);
    }
}