using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public CharacterController characterController;
    public float speed = 12.0f;
    public float gravity = 9.81f;
    public float jumpHeight = 3.0f;
    public Transform groundCheck;
    public float groundDistance;
    public LayerMask groundMask;
    
    Vector3 velocity;
    bool isGrounded;

    bool terrainLoaded = false;

    // Start is called before the first frame update
    void Start()
    {
        terrainLoaded = false;
    }


    void UpdateMovement()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        characterController.Move(move * speed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    // Update is called once per frame
    void Update()
    {
        if (! terrainLoaded)
        {
            Ray ray = new Ray(groundCheck.transform.position, new Vector3(0, -1, 0));
            if (Physics.Raycast(ray, out RaycastHit hit, 1000))
            {
                Debug.Log("HIIIIIIT:" + hit.transform.gameObject.name);
                terrainLoaded = true;
            }
            
            return;
        }
        
        UpdateMovement();
    }
}
