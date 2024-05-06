using PlayerControls;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cutscene
{
    public class CutScene : MonoBehaviour
    {
        [SerializeField]
        DialogueFlow dialogue;
        [SerializeField]
        TextMeshProUGUI dialogueText, speakerName;
        [SerializeField]
        Image leftPortrait, rightPortrait;
        [SerializeField]
        CameraControls cameraControls;
        [SerializeField]
        PlayerInput playerInput;
        // Start is called before the first frame update
        void Start()
        {
            dialogue.Start();
            Time.timeScale = 0;
            playerInput.enabled = false;
            cameraControls.enabled = false;
            SetUI();
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.anyKeyDown)
                SetUI();
        }
        void SetUI()
        {
            Dialogue scene = dialogue.GetNextScene();
            if (scene == null)
            {
                EndCutscene();
                return;
            }
            speakerName.text = scene.CharacterName;
            dialogueText.text = scene.dialogue;
            if (!scene.leftOrRight)
            {
                leftPortrait.sprite = scene.Portrait;
                leftPortrait.enabled = true;
                rightPortrait.enabled = false;
            }
            else
            {
                rightPortrait.sprite = scene.Portrait;
                rightPortrait.enabled = true;
                leftPortrait.enabled = false;
            }
        }
        void EndCutscene()
        {
            gameObject.SetActive(false);
            playerInput.enabled = true;
            cameraControls.enabled = true;
            Time.timeScale = 1;
        }
    }
}