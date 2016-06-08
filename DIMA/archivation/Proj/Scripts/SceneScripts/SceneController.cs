using UnityEngine;

namespace Assets.Scripts.SceneScripts
{
    public class SceneController : MonoBehaviour
    {
        public GameObject[] Ships;
        public GameObject SpawnPoint;
        private float _angle = 0;
        private Vector3 _position;
        private float time = 2;

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (time >= 0)
            {
                time -= Time.deltaTime;
            }
            else
            {
                SpawnShip();
                time = 1;
            }
        }

        public void SpawnShip()
        {
            int y = Random.Range(-610, 650);
            int z = Random.Range(-800, 900);
            _position = new Vector3(0, y, z);
            int randShip = Random.Range(0, Ships.Length);
            GameObject clone = (GameObject)Instantiate(Ships[randShip], SpawnPoint.transform.position + _position, Quaternion.AngleAxis(90, Vector3.up));
        }
    }
}
