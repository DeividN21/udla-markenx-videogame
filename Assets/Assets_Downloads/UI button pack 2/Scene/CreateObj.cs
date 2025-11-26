using UnityEngine;
using UnityEngine.UI;

namespace Assets_Downloads.UI_button_pack_2.Scene
{
	public class CreateObj : MonoBehaviour {

		public Image PP;
		public Sprite[] temp;
		public GameObject parent;
		// Use this for initialization
		void Start () {
			for (int i = 0; i < temp.Length; i++) {
				var createImage = Instantiate(PP) as Image;
				createImage.transform.SetParent(parent.transform, false);
				createImage.sprite = temp [i];
				createImage.gameObject.SetActive (true);
			}

		}
	
		// Update is called once per frame
		void Update () {
		
		}
	}
}
