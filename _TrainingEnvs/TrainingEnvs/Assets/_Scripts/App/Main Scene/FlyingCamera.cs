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

    [Header("Parametri clamp (fixare) camera")]
    [SerializeField] [Tooltip("Transformul obiectului parinte")] Transform parentTransform = null;
    [SerializeField] [Tooltip("Valoarea maxima orizontala")] float orizontalClampValue = 0f;
    [SerializeField] [Tooltip("Valoarea maxima verticala")] float maxVerticalClampValue = 0f;
    [SerializeField] [Tooltip("Valoarea minima verticala")] float minVerticalClampValue = 0f;

    // ----------------------------------------------------------- VARIABILE ---------------------------------------------------- //

    private float rotationX = 0.0f;
    private float rotationY = 0.0f;

    // Proprietate bool care ne permite/interzice miscarea
    public bool CanMoveCamera { get; set; } = true;

    // ------------------------------------------------------------ METODE ------------------------------------------------------ //

    void Update()
    {
        if(CanMoveCamera) ApplyMovement();
    }

    private void FixedUpdate()
    {
        if (CanMoveCamera) ClampCameraPosition();
    }

    void ClampCameraPosition()
    {
        // Clamping camera position
        transform.position = new Vector3(

            // Axa x
            Mathf.Clamp(transform.position.x, parentTransform.position.x - orizontalClampValue, parentTransform.position.x + orizontalClampValue),
            // Axa y
            Mathf.Clamp(transform.position.y, parentTransform.position.y + minVerticalClampValue, parentTransform.position.y + maxVerticalClampValue),
            // Axa z
            Mathf.Clamp(transform.position.z, parentTransform.position.z - orizontalClampValue, parentTransform.position.z + orizontalClampValue)


                                        );
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