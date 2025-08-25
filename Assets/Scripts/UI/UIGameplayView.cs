using UnityEngine;

namespace SimpleFPS
{
	public class UIGameplayView : MonoBehaviour
	{
		public UIKillFeed    KillFeed;
		public RectTransform Fire;

		private void Awake()
		{
			Fire.gameObject.SetActive(Application.isMobilePlatform && Application.isEditor == false);
		}
	}
}
