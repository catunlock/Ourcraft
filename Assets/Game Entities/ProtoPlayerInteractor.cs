using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProtoPlayerInteractor : MonoBehaviour
{
    HumanGameEntity toInteract;
    public GameObject interactImage;
    bool interacting;

    // Start is called before the first frame update
    void Start()
    {
        interacting = false;
        toInteract = null;
    }

    void FixedUpdate()
    {
        if (interactImage.activeSelf && !interacting && toInteract == null)
            interactImage.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (!interacting && toInteract != null && Input.GetKeyDown(KeyCode.E))
        {
            interacting = true;
            toInteract.playerInteracted = true;
            toInteract.transform.LookAt(gameObject.transform);
        }
        else if (interacting && Input.GetKeyDown(KeyCode.E))
        {
            interacting = false;
            toInteract.playerInteracted = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Vector3.Distance(gameObject.transform.position, other.gameObject.transform.position) < 3f)
        {
            if (other.tag.Equals("Merchant"))
            {
                toInteract = other.gameObject.GetComponent<HumanMerchant>();
                interactImage.SetActive(true);
            }
            else if (other.tag.Equals("Mayor"))
            {
                toInteract = other.gameObject.GetComponent<HumanMayor>();
                interactImage.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag.Equals("Merchant") || other.tag.Equals("Mayor"))
        {
            if (interacting)
            {
                interacting = false;
                toInteract.playerInteracted = false;
            }

            toInteract = null;
            interactImage.SetActive(false);
        }
    }

}
