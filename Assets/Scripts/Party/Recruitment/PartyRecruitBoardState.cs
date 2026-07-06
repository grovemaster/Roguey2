using System;
using System.Collections.Generic;
using UnityEngine;

namespace JRogue.Party.Recruitment
{
  public sealed class PartyRecruitBoardState : MonoBehaviour
  {
    public static PartyRecruitBoardState Instance { get; private set; }

    readonly HashSet<string> _recruitedIds = new HashSet<string>(StringComparer.Ordinal);

    void Awake()
    {
      if (Instance != null && Instance != this)
      {
        Destroy(gameObject);
        return;
      }

      Instance = this;
    }

    void OnDestroy()
    {
      if (Instance == this)
        Instance = null;
    }

    public bool IsRecruited(string recruitId) =>
      !string.IsNullOrEmpty(recruitId) && _recruitedIds.Contains(recruitId);

    public void MarkRecruited(string recruitId)
    {
      if (string.IsNullOrEmpty(recruitId))
        return;

      _recruitedIds.Add(recruitId);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public void DevReset() => _recruitedIds.Clear();
#endif
  }
}
