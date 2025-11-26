using UnityEngine;
using UnityEngine.UI;

namespace Managers
{
    public class ConsumerReaction : MonoBehaviour
    {
        [Header("Referencias")]
        public Image imagenConsumidor;

        [Header("Sprites de Reacción")]
        public Sprite spriteEnojado;  // 0% - 40%
        public Sprite spriteNeutral;  // 41% - 75%
        public Sprite spriteFeliz;    // 76% - 100%

        private GameSceneManager manager;

        void Start()
        {
            manager = GameSceneManager.Instance;
            if (imagenConsumidor == null) imagenConsumidor = GetComponent<Image>();
        }

        void Update()
        {
            if (manager == null) return;

            // Actualizar cada frame
            ActualizarExpresion(manager.GetAceptacionActual());
        }

        void ActualizarExpresion(float aceptacion)
        {
            if (aceptacion < 40)
            {
                imagenConsumidor.sprite = spriteEnojado;
            }
            else if (aceptacion < 75)
            {
                imagenConsumidor.sprite = spriteNeutral;
            }
            else
            {
                imagenConsumidor.sprite = spriteFeliz;
            }
        }
    }
}