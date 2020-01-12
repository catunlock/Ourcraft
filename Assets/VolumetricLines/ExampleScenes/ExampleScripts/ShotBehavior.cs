using UnityEngine;
using System.Collections;

public class ShotBehavior : MonoBehaviour {

    public float speed = 1000f;
    public float maxLiveTime = 100.0f;
    public float force = -5f;
    public float range = 0.2f;
    public MeshGenerator meshGenerator;

    // Use this for initialization
    void Start () {
        meshGenerator = FindObjectOfType<MeshGenerator>();
        Destroy(gameObject, maxLiveTime);
    }
	
	// Update is called once per frame
	void Update () {
		transform.position += transform.forward * Time.deltaTime * speed;
	}

    private void OnTriggerEnter(Collider other)
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Collision enter");
        if (collision.gameObject.tag.Equals("chunk"))
        {
            Debug.Log("True");
            meshGenerator.UpdateChunk(collision.GetContact(0).point, force, range);
        }


        Debug.Log("Chocado:" + collision.gameObject.name);
        Destroy(this.gameObject);
    }

    private void OnCollisionExit(Collision collision)
    {
        Debug.Log("Collision exit");
    }
}
