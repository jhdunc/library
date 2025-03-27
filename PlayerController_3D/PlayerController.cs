using System;
using System.Collections;
using System.Linq;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

/// <summary>
/// Script heavily inspired by Matthew J Spencer's Celeste inspired 3D player controller
/// https://github.com/Matthew-J-Spencer/player-controller/blob/main/PlayerController3d.cs
/// </summary>

public class PlayerController : MonoBehaviour, IDamageable
{
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private Animator _anim;
    [SerializeField] private ScriptableStats statsDefault;
    [SerializeField] private ScriptableSoundPlayer soundStatsDefault;
    private ScriptableStats _stats;
    private ScriptableSoundPlayer _soundStats;

    // Local FMOD Attributes
    [HideInInspector] public FMOD.Studio.EventInstance i_Step;
    private FMOD.Studio.EventInstance i_Heartbeat;

    private HealthSystem _healthSystem;
    private FrameInputs _inputs;
    private bool _isInputEnabled;
    private bool _isDamageable = true;

    private void Start()
    {
        _stats = Instantiate(statsDefault);
        _soundStats = Instantiate(soundStatsDefault);
        _isInputEnabled = true;
        _healthSystem = gameObject.GetComponent<HealthManager>().healthSystem;

        // pre-loads all looping audio to ease cpu's performance
        i_Step = AudioManager.Instance.PlayInstance(_soundStats.SFX_Step, transform.position, gameObject);
        i_Heartbeat = AudioManager.Instance.PlayInstance(_soundStats.SFX_Heartbeat, transform.position, gameObject);

        AudioManager.Instance.PauseInstance(i_Step);
        AudioManager.Instance.PauseInstance(i_Heartbeat);

        // set invincibility at start
        StartCoroutine(Invincible(1.2f));
    }
    private void Update()
    {
        if (_isInputEnabled)
        GetInputs();

        HandleGrounding();

        HandleWalking();

        HandleJumping();

        HandleWallSlide();

        HandleWallGrab();

        HandleDashing();
    }
  
    #region Inputs

    private void GetInputs()
    {
        _inputs.RawX = (int)Input.GetAxisRaw("Horizontal");
        _inputs.RawZ = (int)Input.GetAxisRaw("Vertical");
        _inputs.X = Input.GetAxis("Horizontal");
        _inputs.Z = Input.GetAxis("Vertical");

        _dir = new Vector3(_inputs.X, 0, 0);

        // Set look direction only if dir is not zero (moving) - does not snap to "default"
        if (_dir != Vector3.zero && !_grabbing && !_wallSliding) _anim.transform.forward = _dir;

        _anim.SetInteger("RawZ", _inputs.RawZ);
    }

    #endregion

    #region Detection

    [Header("Detection")] 
    private bool _isAgainstWall, _pushingWall;
    public bool IsGrounded;

    private readonly Collider[] _ground = new Collider[1];
    private readonly Collider[] _wall = new Collider[1];

    private void HandleGrounding()
    {
        // Grounder
        var grounded = Physics.OverlapSphereNonAlloc(transform.position + new Vector3(0, _stats.grounderOffset), _stats.grounderRadius, _ground, _stats.groundLayer) > 0;

        if (!IsGrounded && grounded)
        {
            IsGrounded = true;
            _hasJumped = false;
            _hasDashed = false;
            _stats.currentMovementLerpSpeed = 100;
            _anim.SetBool("Grounded", true);

            // TO DO // SOUND // Landing Sound
        }
        else if (IsGrounded && !grounded)
        {
            IsGrounded = false;
            _anim.SetBool("Grounded", false);
            transform.SetParent(null);
        }

        // Wall detection
        _isAgainstWall = Physics.OverlapSphereNonAlloc(WallDetectPosition, _stats.wallCheckRadius, _wall, _stats.groundLayer) > 0;
        _pushingWall = _isAgainstWall && _inputs.X < 0;
    }

    private Vector3 WallDetectPosition => _anim.transform.position + Vector3.up + _anim.transform.forward * statsDefault.wallCheckOffset;


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        // Grounder
        Gizmos.DrawWireSphere(transform.position + new Vector3(0, statsDefault.grounderOffset), statsDefault.grounderRadius);

        // Wall
        Gizmos.DrawWireSphere(WallDetectPosition, statsDefault.wallCheckRadius);
    }

    #endregion

    #region Walking

    [Header("Walking")] 
    private float _currentWalkingPenalty;

    private Vector3 _dir;

    private void HandleWalking()
    {
        _stats.currentMovementLerpSpeed = Mathf.MoveTowards(_stats.currentMovementLerpSpeed, 100, _stats.wallJumpMovementLerp * Time.deltaTime);

        var normalizedDir = _dir.normalized;

        if (_dir != Vector3.zero) {
            // Slowly increase max speed
            _currentWalkingPenalty += _stats.acceleration * Time.deltaTime;
            if (!AudioManager.Instance.IsAudioPlaying(i_Step) && IsGrounded) AudioManager.Instance.ContinueInstance(i_Step);
        }
        else {
            AudioManager.Instance.PauseInstance(i_Step);
            _currentWalkingPenalty -= _stats.acceleration * Time.deltaTime;
        }

        _currentWalkingPenalty = Mathf.Clamp(_currentWalkingPenalty, _stats.maxWalkingPenalty, 1);

        // Set current y vel and add walking penalty
        var targetVel = new Vector3(normalizedDir.x, _rb.linearVelocity.y, normalizedDir.z) * _currentWalkingPenalty * _stats.walkSpeed;

        // Set vel
        var idealVel = new Vector3(targetVel.x, _rb.linearVelocity.y, targetVel.z);

        _rb.linearVelocity = Vector3.MoveTowards(_rb.linearVelocity, idealVel, _stats.currentMovementLerpSpeed * Time.deltaTime);

        _anim.SetBool("Walking", _dir != Vector3.zero && IsGrounded);
    }

    #endregion

    #region Jumping

    [Header("Jumping")] 
    private float _timeLeftGrounded = -10;
    private float _timeLastWallJumped;
    private bool _hasJumped;
    private bool _hasDoubleJumped;

    private void HandleJumping()
    {
        if (Input.GetButtonDown("Jump"))
        {
            if (!IsGrounded && _isAgainstWall)
            {
                _timeLastWallJumped = Time.time;
                _stats.currentMovementLerpSpeed = _stats.wallJumpMovementLerp;

                if (GetWallHit(out var wallHit)) ExecuteJump(new Vector3(wallHit.normal.x * _stats.jumpForce, _stats.jumpForce, wallHit.normal.z * _stats.jumpForce)); // Wall jump
            }
            else if (IsGrounded || Time.time < _timeLeftGrounded + _stats.coyoteTime || _stats.enableDoubleJump && !_hasDoubleJumped)
            {
                if (!_hasJumped || _hasJumped && !_hasDoubleJumped) ExecuteJump(new Vector2(_rb.linearVelocity.x, _stats.jumpForce), _hasJumped); // Ground jump
            }
        }

        void ExecuteJump(Vector3 dir, bool doubleJump = false)
        {
            _rb.linearVelocity = dir;
            // TO DO // Jump FX
            // TO DO // _anim.SetTrigger(doubleJump ? "DoubleJump" : "Jump");
            _hasDoubleJumped = doubleJump;
            AudioManager.Instance.PauseInstance(i_Step);
            if (!_hasDoubleJumped) AudioManager.Instance.PlaySound(_soundStats.SFX_GroundedJump, transform.position);
            else AudioManager.Instance.PlaySound(_soundStats.SFX_AirJump, transform.position);
            _hasJumped = true;
        }

        // Fall faster and allow small jumps. stats.jumpVelocityFalloff is the point at which we start adding extra gravity. Using 0 causes floating
        if (_rb.linearVelocity.y < _stats.jumpVelocityFalloff || _rb.linearVelocity.y > 0 && !Input.GetButton("Fire2"))
            _rb.linearVelocity += _stats.fallMultiplier * Physics.gravity.y * Vector3.up * Time.deltaTime;
    }

    #endregion

    #region Wall Slide

    [Header("Wall Slide")]
    
    private bool _wallSliding;

    private void HandleWallSlide()
    {
        if (_pushingWall && !_wallSliding)
        {
            _wallSliding = true;
            
            // TO DO // 
            // Particles for wall slide (start)?
            
            if (GetWallHit(out var wallHit))
            {
                // Face wall
                _anim.transform.forward = -wallHit.normal;

                // Move closer to wall
                var hitPos = new Vector3(wallHit.point.x, transform.position.y, wallHit.point.z);
                if (Vector3.Distance(transform.position, wallHit.point) > 0.5f) transform.position = Vector3.MoveTowards(transform.position, hitPos, 0.4f);
            }
        }
        else if (!_pushingWall && _wallSliding && !_grabbing)
        {
            _wallSliding = false;
            // TO DO // Particles for slide? (end)
        }

        if (_wallSliding) // Don't add sliding until actually falling or it'll prevent jumping against a wall
            if (_rb.linearVelocity.y < 0)
                _rb.linearVelocity = new Vector3(0, -_stats.slideSpeed);
    }
 
    private bool GetWallHit(out RaycastHit outHit)
    {
        if (Physics.Raycast(_anim.transform.position + Vector3.up, _anim.transform.forward, out var hit, 2, _stats.groundLayer))
        {
            outHit = hit;
            return true;
        }

        outHit = new RaycastHit();
        return false;
    }

    #endregion

    #region Wall Grab
    [SerializeField] private bool _grabbing;

    private void HandleWallGrab()
    {
        var grabbing = _isAgainstWall && Input.GetButton("Jump") && Time.time > _timeLastWallJumped + _stats.wallJumpLock;

        _rb.useGravity = !_grabbing;
        if (grabbing && !_grabbing)
        {
            _grabbing = true;
            // TO DO // FX for wall grab (starts)
        }
        else if (!grabbing && _grabbing)
        {
            _grabbing = false;
            // TO DO // FX for wall grab (ends)
        }

        if (_grabbing) _rb.linearVelocity = new Vector3(0, _inputs.RawZ * _stats.slideSpeed * (_inputs.RawZ < 0 ? 1 : 0.8f));

        _anim.SetBool("Climbing", _wallSliding || _grabbing);
    }

    #endregion

    #region Dash

    [Header("Dash")] 
    

    private bool _hasDashed;
    private bool _dashing;
    private float _timeStartedDash;
    private Vector3 _dashDir;
    private bool _dashingToTarget;

    private void HandleDashing()
    {// "Fire3" current set to Left Shift
        if (Input.GetButtonDown("Fire3") && !_hasDashed)
        {
            _dashDir = new Vector3(_inputs.RawX, 0, 0).normalized;
             if (_dashDir == Vector3.zero) _dashDir = _anim.transform.forward;
           
            if (_stats.useDashTargets)
            {
                var targets = Physics.CapsuleCastAll(transform.position + new Vector3(0, _stats.dashTargetCastExtent) + _anim.transform.forward,
                    transform.position - new Vector3(0, _stats.dashTargetCastExtent) + _anim.transform.forward, _stats.dashTargetCastRadius, _anim.transform.forward, _stats.dashTargetCastDistance, _stats.dashTargetMask);

                var closestTarget = targets.Select(t => t.transform).OrderBy(t => Vector3.Distance(transform.position, t.position)).FirstOrDefault();

                if (closestTarget != null)
                {
                    _dashDir = (closestTarget.position - transform.position).normalized;
                    _dashingToTarget = true;
                }
            }

            _dashing = true;
            _hasDashed = true;
            _timeStartedDash = Time.time;
            _rb.useGravity = false;
            

            AudioManager.Instance.PlaySound(_soundStats.SFX_Dash, transform.position);
            // TO DO // FX / Animation Dash (starts)
        }

        if (_dashing)
        {
            _rb.linearVelocity = _dashDir * _stats.dashSpeed;

            if (Time.time >= _timeStartedDash + _stats.dashLength && !_dashingToTarget)
            {
                _dashing = false;
                // Clamp the velocity so they don't keep shooting off
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, _rb.linearVelocity.y > 3 ? 3 : _rb.linearVelocity.y);
                _rb.useGravity = true;
                if (IsGrounded) _hasDashed = false;
                // TO DO // FX / Animation Dash (ends)
            }
        }
    }

    #endregion

    #region Impacts

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.CompareTag("Death")) Die();
        // TO DO //
        // Animation/FX / Deathy Death
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Death")) Die();
        else if (other.gameObject.layer == LayerMask.NameToLayer("Target"))
            _dashingToTarget = false;
        else // Entering a trigger that is not on the Target layer will reset Dash - it can be used to chain dash from point to point.
            _hasDashed = false;
    }


    #endregion

    #region Damage

    public void TakeDamage(int damageAmount, float stun, GameObject attacker)
    {
        throw new NotImplementedException();
    }
    public void Hurt(int damageAmount, float stun)
    {
        Debug.LogError("No Damage Done: Update Script to use TakeDamage function from IDamageable interface");
    }
    public void TakeDamage(int damageAmount, float stun)
    {
        if (!_isDamageable) return;
        AudioManager.Instance.PlaySound(_soundStats.SFX_GetHit,transform.position);

        i_Heartbeat.setParameterByName("HealthPercentage", _healthSystem.GetHealthPercent());

        if (_healthSystem.GetHealth() <= 6 && _healthSystem.GetHealth() >= 0) AudioManager.Instance.ContinueInstance(i_Heartbeat);
        else AudioManager.Instance.PauseInstance(i_Heartbeat);

        // TO DO // Set Up Invulnerability
        _healthSystem.Damage(damageAmount);
        if (_healthSystem.GetHealth() <= 0)
        {
            Die();
            return;
        }
        // TO DO // enter invulnerable state

        // stop player velocity
        Vector2 newVelocity;
        newVelocity.x = 0;
        newVelocity.y = 0;
        _rb.linearVelocity = newVelocity;

        // TO DO // visual effect (flash white?)
        // TO DO // hurt recoil

        _isInputEnabled = false;
        StartCoroutine(Invincible(_stats.invincibleTime));
        StartCoroutine(RecoverFromHurtCoroutine(stun, stun + 0.5f));
    }
    private IEnumerator Invincible(float duration)
    {
        if (_isDamageable)
        {
            _isDamageable = false;
            float timestep = 0;
            while (timestep < duration)
            {
                timestep += Time.deltaTime;
                Debug.Log("invincible!");
            }
            yield return new WaitForSeconds(duration);
            _isDamageable = true;
            Debug.LogWarning("I can be hurt!");
        }
    }

    private IEnumerator RecoverFromHurtCoroutine(float stunTime, float recoverTime)
    {
        yield return new WaitForSeconds(stunTime);
        _isInputEnabled = true;
        yield return new WaitForSeconds(recoverTime);
        // TO DO // Reset vulneratbility
    }
    private void Die()
    {
        _isInputEnabled = false;
        AudioManager.Instance.ClearInstance(i_Step, i_Heartbeat);
        // TO DO // Death effects, animations
        // TO DO // Death what happens?!
        Debug.Log("You Have Died");
    }
    #endregion

    private struct FrameInputs
    {
        public float X, Z;
        public int RawX, RawZ;
    }
    
    private void OnDestroy() => AudioManager.Instance.ClearInstance(i_Step, i_Heartbeat);

    
}