using System.Collections.Generic;

// 세이브 파일(JSON)로 직렬화되는 순수 데이터 컨테이너.
public class PlayerSaveData
{
  public int Level = 1;
  public int CurrentExp = 0;
  public int MaxExp = 15;
  public int StatPoints = 0;
  public int SkillPoints = 0;
  public string CurrentJob = "Beginner";

  public int Str = 4;
  public int Dex = 4;
  public int Int = 4;
  public int Luk = 4;
  public int Defense = 10;
  public int AttackPower = 10;
  public int MaxHp = 100;
  public int CurrentHp = 100;
  public int MaxMp = 50;
  public int CurrentMp = 50;
  public int Money = 0;

  // "" (empty) means the equipment slot is empty.
  public List<string> EquipmentItemIds = new();
  public Dictionary<string, int> ConsumableCounts = new();
  public Dictionary<string, int> EtcCounts = new();
  // EquipSlot(문자열) -> 장착중인 ItemId. 장착된 슬롯이 없으면 항목이 없다.
  public Dictionary<string, string> EquippedSlotItemIds = new();
  public Dictionary<string, int> AllocatedSkillPoints = new();

  public string MapScenePath = "res://map/Map0.tscn";
  public float PosX;
  public float PosY;
  public float PosZ;
}
