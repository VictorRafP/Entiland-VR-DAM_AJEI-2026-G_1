namespace EntilandVR.DosCuatro.DAM_AJEI.G_Uno
{
    using UnityEngine;
    using UnityEngine.InputSystem;

    public class Shoot : MonoBehaviour
    {
        public GameObject bullet;

        private float shootInterval = 0.4f;
        private float shootTimer = 0f;

        private float bulletSpeed = 25f;

        void Start()
        {
        }

        void Update()
        {
            shootTimer -= Time.deltaTime;

            //if (shoot)
            {
                Quaternion rotation = transform.rotation * Quaternion.Euler(0, 0, 90);
                GameObject newBullet = Instantiate(bullet, transform.position, rotation);

                Rigidbody rbBullet = newBullet.GetComponent<Rigidbody>();
                rbBullet.linearVelocity = transform.TransformDirection(Vector3.right) * bulletSpeed;

                shootTimer = shootInterval;

                Destroy(newBullet, 5f);
            }
        }
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Obstacle"))
            {
                Destroy(bullet);
            }

        }
    }
}