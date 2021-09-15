using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using TournamentAssistantShared.Models;
using UnityEngine;
using static BeatSaberMarkupLanguage.Components.CustomListTableData;

namespace TournamentAssistant.ViewControllers
{
    [ViewDefinition("TournamentAssistant.Views.player-list-view.bsml")]
    [HotReload(RelativePathToLayout = @"..\Views\player-list-view.bsml")]
    internal class PlayerListView : BSMLAutomaticViewController
    {
        [UIComponent("player-list")]
        protected readonly CustomListTableData _playerList = null!;

        public event Action<Player>? PlayerSelected;

        private List<PlayerCellInfo> _players = new();

        private void SetPlayers(List<PlayerCellInfo> value)
        {
            _players = value;
            if (_playerList != null)
            {
                _playerList.data = new List<CustomCellInfo>(_players);
                _playerList.tableView.ReloadData();
            }
        }

        public void SetPlayers(List<Player> players)
        {
            SetPlayers(players.Select(p => new PlayerCellInfo(p)).ToList());
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            if (firstActivation)
            {
                rectTransform.anchorMin = new Vector3(0.5f, 0, 0);
                rectTransform.anchorMax = new Vector3(0.5f, 1, 0);
                rectTransform.sizeDelta = new Vector3(70, 0, 0);
            }
            if (addedToHierarchy)
            {
                SetPlayers(_players);
            }
        }

        [UIAction("player-clicked")]
        protected void PlayerClicked(TableView _, int row)
        {
            PlayerSelected?.Invoke((_playerList.data[row] as PlayerCellInfo)!.Player);
        }

        protected class PlayerCellInfo : CustomCellInfo
        {
            public Player Player { get; set; }

            public PlayerCellInfo(Player player) : base($"{player.Name}")
            {
                Player = player;
            }
        }
    }
}