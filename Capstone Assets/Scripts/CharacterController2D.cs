
// written by Jayanth Tumuluri in 2016

using UnityEngine;
using System.Collections;
using UnityStandardAssets.CrossPlatformInput;

public class CharacterController2D : MonoBehaviour {
	
    [Range(0.0f, 10.0f)]
    public float moveSpeed = 3f;

    public float jumpForce = 600f;
	
    public int playerHealth = 1;
	
    public LayerMask whatIsGround;
	
    public Transform groundCheck;
	
    [HideInInspector]
    public bool playerCanMove = true;
	
    public AudioClip coinSFX;
    public AudioClip deathSFX;
    public AudioClip fallSFX;
    public AudioClip jumpSFX;
    public AudioClip victorySFX;
	
    Transform _transform;
    Rigidbody2D _rigidbody;
    Animator _animator;
    AudioSource _audio;
	
    float _vx;
    float _vy;
	
    bool _facingRight = true;
    bool _isGrounded = false;
    bool _isRunning = false;
    bool _canDoubleJump = false;
	
    int _playerLayer;
	
    int _platformLayer;

    void Awake() {
        _transform = GetComponent<Transform>();

        _rigidbody = GetComponent<Rigidbody2D>();
        if (_rigidbody == null)
            Debug.LogError("Rigidbody2D component missing from this gameobject");

        _animator = GetComponent<Animator>();
        if (_animator == null)
            Debug.LogError("Animator component missing from this gameobject");

        _audio = GetComponent<AudioSource>();
        if (_audio == null) {
            Debug.LogWarning("AudioSource component missing from this gameobject. Adding one.");
            _audio = gameObject.AddComponent<AudioSource>();
        }
		
        _playerLayer = this.gameObject.layer;
		
        _platformLayer = LayerMask.NameToLayer("Platform");
    }
	
    void Update()
    {
        if (!playerCanMove || (Time.timeScale == 0f))
            return;
		
        _vx = CrossPlatformInputManager.GetAxisRaw("Horizontal");
		
        if (_vx != 0)
        {
            _isRunning = true;
        } else {
            _isRunning = false;
        }
		
        _animator.SetBool("Running", _isRunning);
		
        _vy = _rigidbody.velocity.y;
		
        _isGrounded = Physics2D.Linecast(_transform.position, groundCheck.position, whatIsGround);
		
        if (_isGrounded)
        {
            _canDoubleJump = true;
        }
		
        _animator.SetBool("Grounded", _isGrounded);

        if (_isGrounded && CrossPlatformInputManager.GetButtonDown("Jump"))
        {
            DoJump();
        } else if (_canDoubleJump && CrossPlatformInputManager.GetButtonDown("Jump"))
        {
            DoJump();
            _canDoubleJump = false;
        }
		
        if (CrossPlatformInputManager.GetButtonUp("Jump") && _vy > 0f)
        {
            _vy = 0f;
        }
		
        _rigidbody.velocity = new Vector2(_vx * moveSpeed, _vy);
		
        Physics2D.IgnoreLayerCollision(_playerLayer, _platformLayer, (_vy > 0.0f));
    }
	
    void LateUpdate()
    {
        Vector3 localScale = _transform.localScale;

        if (_vx > 0)
        {
            _facingRight = true;
        } else if (_vx < 0) {
            _facingRight = false;
        }
		
        if (((_facingRight) && (localScale.x < 0)) || ((!_facingRight) && (localScale.x > 0))) {
            localScale.x *= -1;
        }
		
        _transform.localScale = localScale;
    }
	
    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.tag == "MovingPlatform")
        {
            this.transform.parent = other.transform;
        }
    }
	
    void OnCollisionExit2D(Collision2D other)
    {
        if (other.gameObject.tag == "MovingPlatform")
        {
            this.transform.parent = null;
        }
    }
	
    void DoJump()
    {
        _vy = 0f;
        _rigidbody.AddForce(new Vector2(0, jumpForce));
        PlaySound(jumpSFX);
    }
	
    void FreezeMotion() {
        playerCanMove = false;
        _rigidbody.isKinematic = true;
    }
	
    void UnFreezeMotion() {
        playerCanMove = true;
        _rigidbody.isKinematic = false;
    }
	
    void PlaySound(AudioClip clip)
    {
        _audio.PlayOneShot(clip);
    }
	
    public void ApplyDamage(int damage) {
        if (playerCanMove) {
            playerHealth -= damage;

            if (playerHealth <= 0) {
                PlaySound(deathSFX);
                StartCoroutine(KillPlayer());
            }
        }
    }
	
    public void FallDeath() {
        if (playerCanMove) {
            playerHealth = 0;
            PlaySound(fallSFX);
            StartCoroutine(KillPlayer());
        }
    }
	
    IEnumerator KillPlayer()
    {
        if (playerCanMove) {
            FreezeMotion();
			
            _animator.SetTrigger("Death");
			
            yield return new WaitForSeconds(2.0f);

            if (GameManager.gm)
                GameManager.gm.ResetGame();
            else
                Application.LoadLevel(Application.loadedLevelName);
        }
    }

    public void CollectCoin(int amount) {
        PlaySound(coinSFX);

        if (GameManager.gm)
            GameManager.gm.AddPoints(amount);
    }

    public void CollectLife(int amount)
    {
        PlaySound(coinSFX);

        if (GameManager.gm)
        {
            GameManager.gm.AddLives(amount);
        }
    }
	
	public void Victory() {
		PlaySound(victorySFX);
		FreezeMotion ();
		_animator.SetTrigger("Victory");

		if (GameManager.gm)
			GameManager.gm.LevelCompete();
	}
	
	public void Respawn(Vector3 spawnloc) {
		UnFreezeMotion();
		playerHealth = 1;
		_transform.parent = null;
		_transform.position = spawnloc;
		_animator.SetTrigger("Respawn");
	}

    public void EnemyBounce()
    {
        DoJump();
    }
}
