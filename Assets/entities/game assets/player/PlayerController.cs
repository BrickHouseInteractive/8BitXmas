﻿using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour {

	//State
	enum State{
		ENTRY,
		MOVE_ONLY,
		THROW_ONLY,
		ACTIVE,
		FALLING,
		DEAD,
		CELEBRATE
	}

	//Public vars
	public AudioClip throwSound;
	public AudioClip catchSound;
	public AudioClip tauntSound;
	public AudioClip multiplier2x;
	public AudioClip multiplier3x;
	public AudioClip multiplier4x;
	public float moveSpeed = 4f;
	public float throwSpeed = 1f;
	public GameObject present;
	public int playerNum = 1;	
	public Sprite presentSprite;
	public string name;
	public Hashtable playerAttrs = new Hashtable();
	public enum Attributes{
		SPEED,
		THROWSPEED,
		FROZEN
	}

	//Private vars
	Animator animator;
	Animator bodyAnimator;
	AudioSource audioSource;	
	bool canThrow = true;
	int catches = 0;
	int playerScore = 0;
	int scoreMultiplier = 1;
	State _state = State.ENTRY;
	Text playerScoreText;
	Text playerNameText;

	// Use this for initialization
	void Awake () {
		animator = gameObject.GetComponent<Animator>();
		audioSource = gameObject.GetComponent<AudioSource>();
		bodyAnimator = transform.Find ("Body").gameObject.GetComponent<Animator>();
		bodyAnimator.logWarnings = false;
		//Get correct score component
		playerScoreText = GameObject.Find("Player "+playerNum+" Score").transform.Find("Score").gameObject.GetComponent<Text>();
		playerNameText = GameObject.Find("Player "+playerNum+" Score").transform.Find("Player Name").gameObject.GetComponent<Text>();
		//Populate Attrs Hashtable
		playerAttrs.Add (Attributes.SPEED, moveSpeed);
		playerAttrs.Add (Attributes.THROWSPEED, throwSpeed);
		playerAttrs.Add (Attributes.FROZEN, State.DEAD);
	}
	
	// Update is called once per frame
	void Update () {
		switch(_state){
		case State.ENTRY:
			break;
		case State.MOVE_ONLY:
			MovePlayer();
			break;
		case State.THROW_ONLY:
			ThrowPresent();
			break;
		case State.ACTIVE:
			MovePlayer();
			ThrowPresent();
			break;
		case State.FALLING:
			break;
		case State.DEAD:
			break;
		}

		//Print buttons for debugging
//		foreach(KeyCode kcode in Enum.GetValues(typeof(KeyCode))){
//			if (Input.GetKeyDown(kcode)) Debug.Log("KeyCode down: " + kcode);
//		}
	}

	void LateUpdate(){
		if(bodyAnimator.GetBool("isThrowing") == true){
			bodyAnimator.SetBool("isThrowing",false);
		}
	}

	//Public Functions
	public void SetCharacter(CharacterCollection.Character character){
		Debug.Log(character.displayName);
		name = playerNameText.text = character.displayName;
		transform.parent.gameObject.GetComponent<SpriteSwitch>().SetSpriteSheet(character.characterSpriteSheetName);
		Sprite[] newSprites = Resources.LoadAll<Sprite>(character.characterSpriteSheetName);
		presentSprite = Array.Find(newSprites, item => item.name == "present");
	}

	public void EnableMovement(){
		_state = State.MOVE_ONLY;
	}
	
	public void EnablePlayer(){
		_state = State.ACTIVE;
	}

	public void FallPlayer(){
		_state = State.FALLING;
		animator.SetBool("FALLING",true);
		transform.Find("Body/Flames").gameObject.GetComponent<SpriteRenderer>().enabled = false;
	}

	public void KillPlayer(){
		_state = State.DEAD;
		bodyAnimator.SetBool("DEAD",true);
	}

	public void DeclareWinner(){
		_state = State.CELEBRATE;
	}
	
	public int IncrementScore(int score){
		catches++;
		switch (catches){
		case 4:
			scoreMultiplier = 2;
			PlaySound(multiplier2x);
			break;
		case 6:
			scoreMultiplier = 3;
			PlaySound(multiplier3x);
			break;
		case 8:
			scoreMultiplier = 4;
			PlaySound(multiplier4x);
			break;
		}
		PlaySound(catchSound);

		int scoreIncrement = score*scoreMultiplier;
		playerScore += scoreIncrement;
		playerScoreText.text = playerScore.ToString();
		return scoreMultiplier;
	}
	
	public void RemoveMultiplier(){
		scoreMultiplier = 1;
		catches = 0;
	}

	public int GetMultipiler(){
		return scoreMultiplier;
	}

	public int GetScore(){
		return playerScore;
	}

	public string GetName(){
		return name;
	}

	public void ApplyPowerup(Attributes attribute, float multiplier, float timeout){
		Debug.Log ("Increase "+name+" "+attribute+" by "+multiplier.ToString());
		//TODO: Add in switch statement for different object types (float and bool at least)
		StartCoroutine(PowerupTimeout(attribute, multiplier, timeout));
	}

	//Private Functions
	void MovePlayer(){
		if(Input.GetButton("Horizontal_P"+playerNum)){
			float deltaX = 0;
			if(Input.GetAxis("Horizontal_P"+playerNum) > 0 && Camera.main.WorldToScreenPoint(transform.position).x < Screen.width*0.975f){
				deltaX = (float) playerAttrs[Attributes.SPEED];
				transform.localScale = new Vector3(1,1,1);
			}else if(Input.GetAxis("Horizontal_P"+playerNum) < 0 && Camera.main.WorldToScreenPoint(transform.position).x > Screen.width*0.025f){
				deltaX = -(float) playerAttrs[Attributes.SPEED];
				transform.localScale = new Vector3(-1,1,1);
			}	
			transform.position += new Vector3(deltaX,0,0)*Time.deltaTime;
		}
	}

	void ThrowPresent(){
		if(Input.GetButton("Throw_P"+playerNum) && canThrow){
			//Play throw sound
			PlaySound(throwSound);
			bodyAnimator.SetBool("isThrowing",true);

			//Create Present
			Vector3 initialPosition = transform.position + new Vector3(8*transform.localScale.x,-5,0);
			GameObject newPresent = Instantiate(present, initialPosition, Quaternion.identity) as GameObject;
			PresentController presentController = newPresent.GetComponent<PresentController>();
			presentController.SetThrower(gameObject);
			presentController.SetPresentSprite(presentSprite);
			canThrow = false;

			//Prevent player from being able to throw immidiately 
			StartCoroutine(ThrowCooldown());
		}
	}


	IEnumerator ThrowCooldown(){
		yield return new WaitForSeconds((float) playerAttrs[Attributes.THROWSPEED]);
		canThrow = true;
	}

	IEnumerator PowerupTimeout(Attributes attribute, float multiplier, float timeout){
		var attributeType = playerAttrs[attribute].GetType();
		if(attributeType == typeof(float)){
			playerAttrs[attribute] = (float) playerAttrs[attribute] * multiplier;
			yield return new WaitForSeconds(timeout);
			playerAttrs[attribute] = (float) playerAttrs[attribute] /  multiplier;
		}else if(attributeType == typeof(State)){
			_state = (State)playerAttrs[attribute];
			yield return new WaitForSeconds(timeout);
			if(_state == (State)playerAttrs[attribute]) _state = State.ACTIVE;
		}
	}

	void PlaySound(AudioClip sound){
		audioSource.PlayOneShot(sound);
	}

}
