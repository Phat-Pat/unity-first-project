﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using Random=UnityEngine.Random;
public class CharacterControl : MonoBehaviour
{

  public GameObject readyObj, p1, p2, mana1, mana2, GameOver, charge1, charge2, countdownObj;
  public float countdown, startTime;
  public int gameMode, player; // 0 local, 1 computer, 2 multiplayer
  private string p1choice, p2choice;
  private int p1power, p2power;
  private bool ready, gameOver, haveAIMove, uploaded, downloaded;
  private Animator p1anim, p2anim;

  void Start() {
    readyObj.SetActive(true);
    p1anim = p1.GetComponent<Animator>();
    p2anim = p2.GetComponent<Animator>();
    GameOver.SetActive(false);
    countdownObj.SetActive(false);
    charge1.GetComponent<ParticleSystem>().Stop();
    charge2.GetComponent<ParticleSystem>().Stop();
    gameMode = GameSelect.GlobalGame;
    p1power = 1; p2power = 1; p1choice = "Blast"; p2choice = "Blast";
  }

  void Update() {
    if (!ready && !readyObj.activeSelf) {
      ready = true;
      startTime = Time.time;
      Debug.Log(startTime);
    }
    if (!gameOver && ready) {
      if (Input.GetKeyDown(KeyCode.Z)) p1choice = "Blast";
      if (Input.GetKeyDown(KeyCode.X)) p1choice = "Charge";
      if (Input.GetKeyDown(KeyCode.C)) p1choice = "Shield";
      GetP2Inputs(gameMode);
      if (Time.time < startTime) {
        if (countdownObj.activeSelf) countdownObj.SetActive(false);
      } else if (Time.time - startTime < countdown) {
        if (!countdownObj.activeSelf) countdownObj.SetActive(true);
        countdownObj.GetComponent<Text>().text = Math.Ceiling(countdown - (Time.time - startTime)).ToString();
      // Networking: add 1sec buffer after time cutoff to send p1choice and receive p2choice
      } else if (gameMode == 2 && Time.time - startTime < countdown + 2) {
        // Upload in first half
        if (Time.time - startTime - countdown < 1) {
          if (!uploaded) {
            Debug.Log(p1choice);
            StartCoroutine(Send());
            uploaded = true;
            downloaded = false;
          }
        // Download in second half
        } else {
          if (!downloaded) {
            StartCoroutine(Receive());
            uploaded = false;
            downloaded = true;
          }
        }
      } else {
        p1anim.Play(p1choice);
        p2anim.Play(p2choice);
        
        switch (p1choice) {
          case "Blast":
            if (p2choice == "Blast") {
              if (p1power > p2power) {
                p2anim.SetBool("Dead", true);
              } 
              if (p1power < p2power) {
                p1anim.SetBool("Dead", true);
              }
            }
            if (p1power == 0) break;
            if (p2choice == "Charge") {
              p2anim.SetBool("Dead", true);
            }
            if (p2choice == "Shield" && p1power >= 3)
              p2anim.SetBool("Dead", true);
            p1power = 0;
            break;

          case "Charge":
            charge1.GetComponent<ParticleSystem>().Play();
            p1power++;
            if (p2choice == "Blast" && p2power > 0) p1anim.SetBool("Dead", true);
            break;
          
          case "Shield":
            if (p2power >= 3) p1anim.SetBool("Dead", true);
            break;

          default:
            break;
        }
        if (p2choice == "Charge") {
          p2power++;
          charge2.GetComponent<ParticleSystem>().Play();
        }
        if (p2choice == "Blast") p2power = 0;
        if (p1anim.GetBool("Dead") || p2anim.GetBool("Dead")) gameOver = true;
        mana1.GetComponent<Text>().text = p1power.ToString(); mana2.GetComponent<Text>().text = p2power.ToString();
        if (!gameOver) {
          startTime = Time.time + 2;
        } else {
          countdownObj.SetActive(false);
        }
        haveAIMove = false;
      }
    }
  }

  void GetP2Inputs(int gameMode) {
    switch (gameMode) {
      case 0: // Local Multiplayer
        if (Input.GetKeyDown(KeyCode.LeftArrow)) p2choice = "Blast";
        if (Input.GetKeyDown(KeyCode.DownArrow)) p2choice = "Charge";
        if (Input.GetKeyDown(KeyCode.RightArrow)) p2choice = "Shield";
        break;
      case 1: // Local vs. AI
        if (!haveAIMove) {
          GetAiMove(p2power);
          haveAIMove = true;
        }
        break;
      case 2: // Online Multiplayer
        // Only want to get inputs once, after the countdown
        break;
      default:
        break;
    }
  }
  void GetAiMove(int p2power) {
    if (p2power < 1) {
      if(Random.Range(0,1) == 0){
        p2choice = "Charge";
      }
      else{
        p2choice = "Shield";
      }
    }
    if (p2power >= 1){
      if(Random.Range(0,2) == 0){
        p2choice = "Shield";
      }
      else if(Random.Range(0,2) == 1){
        p2choice = "Charge";
      }
      else{
        p2choice = "Blast";
      }
    }
    if(p2power >= 3){
      p2choice = "Blast";
    }
  }

  IEnumerator Send() {
    WWWForm form = new WWWForm();
    form.AddField("player", player);
    form.AddField("input", p1choice);

    UnityWebRequest www = UnityWebRequest.Post("https://patrickday.dev/standoff/send", form);
    yield return www.SendWebRequest();

    if (www.result != UnityWebRequest.Result.Success)
    {
      Debug.Log(www.error);
    }
    else
    {
      Debug.Log("Successfully sent data.");
    }
  }
  IEnumerator Receive() {
    WWWForm form = new WWWForm();
    form.AddField("player", player);
    UnityWebRequest www = UnityWebRequest.Post("https://patrickday.dev/standoff/receive", form);
    yield return www.SendWebRequest();

    if (www.result != UnityWebRequest.Result.Success) { Debug.Log(www.error); }
    else { 
    Debug.Log("Successfully received data.");
    p2choice = www.downloadHandler.text;
    }
  }
}
  