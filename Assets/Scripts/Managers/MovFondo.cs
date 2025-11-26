using UnityEngine;
using UnityEngine.UI;

namespace Managers
{
    public class MovFondo : MonoBehaviour
    {
        public RawImage _img;
        public float _x;
        public float _y;

        // Para movimiento del fondo
        void Update()
        {
            _img.uvRect = new Rect(_img.uvRect.position + new Vector2(_x, _y) * Time.deltaTime, _img.uvRect.size);
        }
    }
}
