using System;
using System.Threading.Tasks;
using UnityEngine;
using DolbyIO.Comms;
using DolbyIO.Comms.Unity;
using Newtonsoft.Json;

public class DemoEventNames {
    public static readonly string OnStage = "OnStage";
}

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */
[RequireComponent(typeof(CharacterController))]
public class ParticipantController : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("Move speed of the character in m/s")]
    public float MoveSpeed = 2.0f;

    [Tooltip("Sprint speed of the character in m/s")]
    public float SprintSpeed = 5.335f;

    [Tooltip("How fast the character turns to face movement direction")]
    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f;

    [Tooltip("Acceleration and deceleration")]
    public float SpeedChangeRate = 10.0f;

    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    [Space(10)]
    [Tooltip("The height the player can jump")]
    public float JumpHeight = 1.2f;

    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float Gravity = -15.0f;

    [Space(10)]
    [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
    public float JumpTimeout = 0.50f;

    [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
    public float FallTimeout = 0.15f;

    [Header("Player Grounded")]
    [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
    public bool Grounded = true;

    [Tooltip("Useful for rough ground")]
    public float GroundedOffset = -0.14f;

    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    public float GroundedRadius = 0.28f;

    [Tooltip("What layers the character uses as ground")]
    public LayerMask GroundLayers;

    [Tooltip("How far in degrees can you move the camera up")]
    public float TopClamp = 70.0f;

    [Tooltip("How far in degrees can you move the camera down")]
    public float BottomClamp = -30.0f;

    public bool Jump = false;
    public bool IsLocalPlayer = false;
    public bool Speaking = false;

    public Participant Participant;

    // player
    private float _speed;
    private float _animationBlend;
    private float _targetRotation = 0.0f;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;

    // timeout deltatime
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    // animation IDs
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;
    private int _animIDSpeaking;

    private Animator _animator;
    private CharacterController _controller;

    private const float _threshold = 0.01f;

    private bool _hasAnimator;

    private Vector3? _target = null;
    private Vector3 _direction;

    private DolbyIOSDK _sdk = DolbyIOManager.Sdk;
    private string _conferenceId = "";

    private void Awake()
    {

    }

    private void Start()
    {
        _hasAnimator = TryGetComponent(out _animator);
        _controller = GetComponent<CharacterController>();
        
        AssignAnimationIDs();

        // reset our timeouts on start
        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
    }

    public void Init(string conferenceId, Participant p)
    {
        _conferenceId = conferenceId;
        Participant = p;
    }

    private void Update()
    {
        _hasAnimator = TryGetComponent(out _animator);

        JumpAndGravity();
        GroundedCheck();
        Speak();

        if (_target.HasValue)
        {
            Move();
        }
    }

    public void MoveToWorldCoordinates(Vector3 target)
    {
        _target = target;
    }

    public void LookAt(Vector3 target)
    {
        _direction = target;
    }

    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        _animIDSpeaking = Animator.StringToHash("Speaking");
    }

    private void GroundedCheck()
    {
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
            transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
            QueryTriggerInteraction.Ignore);

        // update animator if using character
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDGrounded, Grounded);
        }
    }

    private void Speak()
    {
        if (_hasAnimator)
        {
            if (Speaking)
            {
                _animator.SetBool(_animIDSpeaking, true);
            }
            else
            {
                _animator.SetBool(_animIDSpeaking, false);
            }
        }
    }

    private void Move()
    {
        Vector3 offset = _target.Value - transform.position;

        if (offset.magnitude > 50.0f) // Spawn location 
        {
            _controller.transform.position = _target.Value;
            return;
        }

// set target speed based on move speed, sprint speed and if sprint is pressed
        float targetSpeed = SprintSpeed;

        // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

        // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        // if there is no input, set the target speed to 0
        if (new Vector2(offset.x, offset.z).magnitude <= 0.3) {
            targetSpeed = 0.0f;
            //print("Arrived at destiation");
        }

        // a reference to the players current horizontal velocity
        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

        //float speedOffset = 0.1f;
        float inputMagnitude = 1f;

        // // accelerate or decelerate to target speed
        // if (currentHorizontalSpeed < targetSpeed - speedOffset ||
        //     currentHorizontalSpeed > targetSpeed + speedOffset)
        // {
        //     // creates curved result rather than a linear one giving a more organic speed change
        //     // note T in Lerp is clamped, so we don't need to clamp our speed
        //     _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
        //         Time.deltaTime * SpeedChangeRate);

        //     // round speed to 3 decimal places
        //     _speed = Mathf.Round(_speed * 1000f) / 1000f;
        // }
        // else
        // {
            _speed = targetSpeed;
        //}
        //print(_speed + " " + targetSpeed);
        _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
        if (_animationBlend < 0.01f) _animationBlend = 0f;

        // normalise input direction
        Vector3 inputDirection = new Vector3(offset.x, 0.0f, offset.z).normalized;

        // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        // if there is a move input rotate player when the player is moving
        if (new Vector2(offset.x, offset.z).magnitude > 0.3)
        {
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                RotationSmoothTime);

            // rotate to face input direction relative to camera position
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0.0f, _direction.y, 0.0f);
        }

        Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
        
        // // move the player
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                                new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

        // update animator if using character
        if (_hasAnimator)
        {
            _animator.SetFloat(_animIDSpeed, _animationBlend);
            _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
        }
    }

    private void JumpAndGravity()
    {
        if (Grounded)
        {
            // reset the fall timeout timer
            _fallTimeoutDelta = FallTimeout;

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);
            }

            // stop our velocity dropping infinitely when grounded
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }

            // Jump
            if (Jump && _jumpTimeoutDelta <= 0.0f)
            {
                Jump = false;
                // the square root of H * -2 * G = how much velocity needed to reach desired height
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, true);
                }
            }

            // jump timeout
            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            // reset the jump timeout timer
            _jumpTimeoutDelta = JumpTimeout;

            // fall timeout
            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDFreeFall, true);
                }
            }

            // if we are not grounded, do not jump
            Jump = false;
        }

        // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += Gravity * Time.deltaTime;
        }
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void OnDrawGizmosSelected()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        if (Grounded) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
        Gizmos.DrawSphere(
            new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
            GroundedRadius);
    }

    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            if (FootstepAudioClips.Length > 0)
            {
                var index = UnityEngine.Random.Range(0, FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            Debug.DrawRay(contact.point, contact.normal, Color.white);
        }

        // if (collision.relativeVelocity.magnitude > 2)
        //     audioSource.Play();

        if (collision.transform.parent && 
            (collision.transform.parent.name.Equals("StageLeft") ||
            collision.transform.parent.name.Equals("StageRight")))
        {
            Jump = true;
        }
    }

    void OnTriggerEnter(Collider collider) {
        Debug.Log($"OnTrigger: {collider.gameObject.transform.parent.name}");
        var gameObject = GameObject.Find("ApplicationManager");

        ApplicationManager manager = gameObject.GetComponent<ApplicationManager>();
        if (Helpers.DecodeMetadata(Participant.Info.ExternalId) != null) // Bots only
        {
            manager.EnterStage(Participant.Id, collider.gameObject.transform.parent.name);
        }
    }

    void OnTriggerExit(Collider collider)
    {
        var gameObject = GameObject.Find("ApplicationManager");

        ApplicationManager manager = gameObject.GetComponent<ApplicationManager>();
        manager.ExitStage(Participant.Id, collider.gameObject.transform.parent.name);
    }
}