using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Quantum;

namespace SimpleFPS
{
	public class UIWeapons : MonoBehaviour
	{
		public Image           WeaponIcon;
		public Image           WeaponIconShadow;
		public TextMeshProUGUI WeaponName;
		public TextMeshProUGUI ClipAmmo;
		public TextMeshProUGUI RemainingAmmo;
		public Image           AmmoProgress;
		public GameObject      NoAmmoGroup;
		public CanvasGroup[]   WeaponThumbnails;

		private GameUI _gameUI;
		private WeaponView _weapon;

		private int _lastClipAmmo;
		private int _lastRemainingAmmo;

		public void UpdateWeapons(WeaponsView weaponsView)
		{
			var weapons = weaponsView.GetPredictedQuantumComponent<Weapons>();


			SetWeapon(weaponsView.VisibleWeapon);

			// Update weapon thumbnails.
			for (int i = 0; i < WeaponThumbnails.Length; i++)
			{
				var weaponRef = weapons.WeaponRefs.Length < i ? weapons.WeaponRefs[i] : EntityRef.None;
				if (weaponRef.IsValid)
				{
					var weapon = _gameUI.Frame.Get<Weapon>(weaponRef);
					WeaponThumbnails[i].alpha = weapon.IsCollected && weapon.HasAmmo ? 1f : 0.2f;
				}
				else
				{
					WeaponThumbnails[i].alpha = 0f;
				}
			}

			if (_weapon == null)
				return;

			var quantumWeapon = _gameUI.Frame.Get<Weapon>(weapons.CurrentWeapon);
			UpdateAmmoProgress(ref weapons, ref quantumWeapon);

			// Modify UI text only when value changed.
			if (quantumWeapon.ClipAmmo == _lastClipAmmo && quantumWeapon.RemainingAmmo == _lastRemainingAmmo)
				return;

			ClipAmmo.text = quantumWeapon.ClipAmmo.ToString();
			RemainingAmmo.text = quantumWeapon.RemainingAmmo < 1000 ? quantumWeapon.RemainingAmmo.ToString() : "-";

			NoAmmoGroup.SetActive(quantumWeapon.ClipAmmo == 0 && quantumWeapon.RemainingAmmo == 0);

			_lastClipAmmo = quantumWeapon.ClipAmmo;
			_lastRemainingAmmo = quantumWeapon.RemainingAmmo;
		}

		private void SetWeapon(WeaponView weapon)
		{
			if (_weapon == weapon)
				return;

			if (weapon != null)
			{
				WeaponIcon.sprite = weapon.Icon;
				WeaponIconShadow.sprite = weapon.Icon;
				WeaponName.text = weapon.Name;
			}

			_weapon = weapon;
		}

		private void UpdateAmmoProgress(ref Weapons weapons, ref Weapon currentWeapon)
		{
			if (weapons.IsReloading)
			{
				// During reloading the ammo progress bar slowly fills.
				AmmoProgress.fillAmount = 1f - weapons.ReloadCooldown.AsFloat / currentWeapon.ReloadTime.AsFloat;
			}
			else
			{
				AmmoProgress.fillAmount = currentWeapon.ClipAmmo / (float)currentWeapon.MaxClipAmmo;
			}
		}

		private void Awake()
		{
			_gameUI = GetComponentInParent<GameUI>();
		}
	}
}
