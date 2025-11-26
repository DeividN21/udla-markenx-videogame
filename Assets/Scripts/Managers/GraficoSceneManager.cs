using UnityEngine;
using UnityEngine.SceneManagement;

namespace Managers
{
    public class GraficoSceneManager : MonoBehaviour
    {
        // El nombre de la escena de reporte a la que se desea volver
        public string escenaReporte = "EndGameScene";

        public void VolverAReporte()
        {
            SceneManager.LoadScene(escenaReporte);
        }
    }
}