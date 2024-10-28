using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tutorial
{
    public class TutorialDealCounter : DealCounter
    {
        public TutorialPlayerZone tutorial;

        public AudioSource _audioSource;

        // Override to avoid responding to GameManager events
        private void OnEnable()
        {
            
        }

        // Override to avoid responding to GameManager events
        private void OnDisable()
        {
            
        }

        private void Update()
        {
            
        }

        public override void SpawnDealAnimEvent()
        {
            var item = tutorial.SpawnDeal();
            item.transform.position = _spawnPoint.position;
            IsDealPresent = true;
            _currentDealItem = item.GetComponent<TutorialItemBehaviour>();
            _currentDealItem.onItemGrabbed += DetectDealPickedUp;
            _currentDealItem.onItemGrabbed += AdvanceTutorialOnce;
            _animator.SetBool(_animIDdealPresent, true);
            _animator.SetFloat(_animIDdespawnSecondsRemaining, 9999);

            tutorial.AdvanceSubstage();
            onDealSpawn?.Invoke();

            _audioSource.PlayOneShot(_spawnSound);
        }

        protected override IEnumerator _SpawnDeals()
        {
            // Wait before starting to wind up
            yield return new WaitForSeconds(_spawnInterval + UnityEngine.Random.Range(-_spawnVariance, _spawnVariance));

            // Start wind up animation
            tutorial.Player.GetComponentInChildren<TutorialDealWarning>().ShowGraphic();
            _animator.SetTrigger(_animIDStartWindupTrigger);
            // Deal item will be spawned from animation event
        }

        public void AdvanceTutorialOnce()
        {
            tutorial.AdvanceSubstage();
            _currentDealItem.onItemGrabbed -= AdvanceTutorialOnce;
        }

        public void ReturnDeal()
        {
            _currentDealItem.transform.position = _spawnPoint.position;
            IsDealPresent = true;
            _currentDealItem.onItemGrabbed += DetectDealPickedUp;
        }
    }
}