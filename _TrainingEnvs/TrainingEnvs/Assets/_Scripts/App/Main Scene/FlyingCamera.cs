using UnityEngine;
using System.Collections;
 
public class FlyingCamera : MonoBehaviour
{
    /*
        Mouse: Ofera directia de inaintare
		WASD/Arrows: Miscare fata , spate , stanga , dreapta
		Q: Urca 
		E: Coboara
        Shift: Viteza marita
        Control: Viteza scazuta
	*/

    // ------------------------------------------------ VARIABILE VIZIBILE IN EDITOR --------------------------------------------- //

    [Header("Parametrii deplasare")]
    [SerializeField][Tooltip("Sensibilitate")] float sensitivity = 90;
    [SerializeField][Tooltip("Viteza verticala")] float verticalSpeed = 4;
    [SerializeField][Tooltip("Viteza orizontala")] float horizontalSpeed = 10;
    [SerializeField][Tooltip("Factor de incetinire")] float slowingFactor = 0.25f;
    [SerializeField][Tooltip("Factor de accelerare")] float accelerationFactor = 3;

    // ----------------------------------------------------------- VARIABILE ---------------------------------------------------- //

    private float rotationX = 0.0f;
    private float rotationY = 0.0f;

    // ------------------------------------------------------------ METODE ------------------------------------------------------ //

    void Update()
    {
        ApplyMovement();
    }

    void ApplyMovement()
    {
        rotationX += Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
        rotationY += Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;
        rotationY = Mathf.Clamp(rotationY, -90, 90);

        transform.localRotation = Quaternion.AngleAxis(rotationX, Vector3.up);
        transform.localRotation *= Quaternion.AngleAxis(rotationY, Vector3.left);

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            transform.position += transform.forward * (horizontalSpeed * accelerationFactor) * Input.GetAxis("Vertical") * Time.deltaTime;
            transform.position += transform.right * (horizontalSpeed * accelerationFactor) * Input.GetAxis("Horizontal") * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            transform.position += transform.forward * (horizontalSpeed * slowingFactor) * Input.GetAxis("Vertical") * Time.deltaTime;
            transform.position += transform.right * (horizontalSpeed * slowingFactor) * Input.GetAxis("Horizontal") * Time.deltaTime;
        }
        else
        {
            transform.position += transform.forward * horizontalSpeed * Input.GetAxis("Vertical") * Time.deltaTime;
            transform.position += transform.right * horizontalSpeed * Input.GetAxis("Horizontal") * Time.deltaTime;
        }


        if (Input.GetKey(KeyCode.Q)) { transform.position += transform.up * verticalSpeed * Time.deltaTime; }
        if (Input.GetKey(KeyCode.E)) { transform.position -= transform.up * verticalSpeed * Time.deltaTime; }
    }
}