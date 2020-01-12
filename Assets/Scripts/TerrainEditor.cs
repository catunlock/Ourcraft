using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainEditor : MonoBehaviour
{
    public bool addTerrain = true;
    public float force = 2f;
    public float range = 0.2f;
    public float maxReachDistance = 100f;

    public Transform playerCamera;
    public MeshGenerator meshGenerator;
    // Start is called before the first frame update
    void Start()
    {
        meshGenerator = FindObjectOfType<MeshGenerator>();
    }


    // Update is called once per frame
    private void FixedUpdate()
    {
        TryEditTerrain();
    }

    private void TryEditTerrain()
    {
        if (force <= 0 || range <= 0)
        {
            return;
        }

        // && elapsedTime > minTime
        if (Input.GetButton("Fire2") )
        {
            RaycastToTerrain(addTerrain);
        }

        if (Input.GetButton("Fire1"))
        {
            RaycastToTerrain(!addTerrain);
        }
    }

    private void RaycastToTerrain(bool addTerrain)
    {
        Vector3 startP = playerCamera.position;
        Vector3 destP = startP + playerCamera.forward;
        Vector3 direction = destP - startP;

        Ray ray = new Ray(startP, direction);

        if (Physics.Raycast(ray, out RaycastHit hit, maxReachDistance) && hit.transform.tag.Equals("chunk"))
        {
            Vector3 hitPoint = hit.point;

            if (addTerrain)
            {
                Collider[] hits = Physics.OverlapSphere(hitPoint, range / 2f * 0.8f);
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].CompareTag("Player"))
                    {
                        return;
                    }
                }
            }

            //GameObject chunk = hit.collider.gameObject;
            if (addTerrain)
            {
                meshGenerator.UpdateChunk(hitPoint, force, range);
            } else
            {
                meshGenerator.UpdateChunk(hitPoint, -force, range);
            }
            
            //EditTerrain(hitPoint, addTerrain, force, range);
        } else
        {
            //Debug.Log("Nothing!");
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draws a 5 unit long red line in front of the object
        Gizmos.color = Color.red;
        Vector3 direction = transform.TransformDirection(Vector3.forward) * maxReachDistance;
        Gizmos.DrawRay(transform.position, direction);

        Vector3 startP = playerCamera.position;

        Ray ray = new Ray(startP, direction);
        if (Physics.Raycast(ray, out RaycastHit hit, maxReachDistance))
        {
            Vector3 hitPoint = hit.point;
            Color col = Color.red;
            col.a = 0.25f;
            Gizmos.color = col;
            Gizmos.DrawSphere(hitPoint, range);
        }

           
    }
}
