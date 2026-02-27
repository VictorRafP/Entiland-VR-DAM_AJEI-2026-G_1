namespace EntilandVR.DosCuatro.DAM_AJEI.G_Uno
{
    using UnityEngine;

    public class Targers : MonoBehaviour
    {
        private MeshRenderer model;
        [SerializeField] private Collider targetCollider;

        private bool destroyed;
        [SerializeField] private float countdown = 3f;
        [SerializeField] private float initialCountdown = 3f;

        [SerializeField] private GameManager gameManager;

        [SerializeField] private Transform[] pivots;
        private float speed;
        [SerializeField] private int pivotsList = 0;

        void Start()
        {
            model = GetComponent<MeshRenderer>();
        }

        void Update()
        {
            TargetHitted();
            TargetMovement();


        }

        private void TargetHitted()
        {
            if (destroyed == true)
            {
                countdown -= Time.deltaTime;
                if (countdown <= 0)
                {
                    model.enabled = true;

                    targetCollider.enabled = true;

                    countdown = initialCountdown;
                    destroyed = false;
                }
            }
        }

        private void SpeedHandler()
        {
            if (GameManager.totalPoints <= 0)
            {
                speed = 0;
            }
            else if(GameManager.totalPoints > 0)
            {
                speed = 1;
            }
            else if (GameManager.totalPoints >= 1000)
            {
                speed = 2;
            }
            else if (GameManager.totalPoints >= 3000)
            {
                speed = 3;
            }
            else if (GameManager.totalPoints >= 4000)
            {
                speed = 4;
            }
        }

        private void TargetMovement()
        {
            if (!destroyed && pivots.Length > 0)
            {
                Transform destinationPivot = pivots[pivotsList];
                SpeedHandler();
                transform.position = Vector3.MoveTowards(transform.position, destinationPivot.position, speed * Time.deltaTime);

                if (Vector3.Distance(transform.position, destinationPivot.position) < 0.1f)
                {
                    pivotsList++;
                    if (pivotsList >= pivots.Length)
                        pivotsList = 0;
                }
            }
        }
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Bullet"))
            {
                model.enabled = false;
                targetCollider.enabled = false;

                Quaternion rotation = transform.rotation * Quaternion.Euler(0, 90, 90);

                destroyed = true;

                if (this.gameObject.tag == "bandit")
                {
                    gameManager.AddPoints(100);
                }
                if (this.gameObject.tag == "specialBandit")
                {
                    gameManager.AddPoints(500);
                }
                if (this.gameObject.tag == "deadly")
                {
                    gameManager.AddLives(-1);
                }                
                if (this.gameObject.tag == "restoreLive")
                {
                    gameManager.AddLives(1);
                }
            }
        }
    }
}