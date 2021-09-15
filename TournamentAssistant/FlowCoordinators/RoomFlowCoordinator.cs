using HMUI;
using IPA.Loader;
using IPA.Utilities;
using IPA.Utilities.Async;
using SiraUtil.Tools;
using System;
using System.Linq;
using System.Threading.Tasks;
using TournamentAssistant.Models;
using TournamentAssistant.ViewControllers;
using TournamentAssistantShared.Models;
using TournamentAssistantShared.Models.Packets;
using Zenject;

namespace TournamentAssistant.FlowCoordinators
{
    internal abstract class RoomFlowCoordinator : FlowCoordinator
    {
        protected CoreServer? _host;
        public event Action? DismissRequested;
        protected MenuLightsPresetSO? _defaultLightsPreset;
        protected MenuLightsPresetSO? _resultsClearedLightsPreset;

        [Inject]
        protected readonly SiraLog _siraLog = null!;

        [Inject]
        protected readonly PluginClient _pluginClient = null!;

        [Inject]
        protected readonly PlayerDataModel _playerDataModel = null!;

        [Inject]
        protected readonly MenuLightsManager _menuLightsManager = null!;

        [Inject]
        private readonly OngoingGameListView _ongoingGameListView = null!;

        [Inject]
        protected readonly MenuTransitionsHelper _menuTransitionsHelper = null!;

        [Inject]
        protected readonly ResultsViewController _resultsViewController = null!;

        [Inject]
        private readonly GameplaySetupViewController _gameplaySetupViewController = null!;

        [Inject]
        private readonly SoloFreePlayFlowCoordinator _soloFreePlayFlowCoordinator = null!;

        protected void Start()
        {
            _defaultLightsPreset = _soloFreePlayFlowCoordinator.GetField<MenuLightsPresetSO, SoloFreePlayFlowCoordinator>("_defaultLightsPreset");
            _resultsClearedLightsPreset = _soloFreePlayFlowCoordinator.GetField<MenuLightsPresetSO, SoloFreePlayFlowCoordinator>("_resultsClearedLightsPreset");
        }

        public void SetHost(CoreServer host)
        {
            _host = host;
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            if (_host is null)
            {
                throw new Exception($"The {nameof(TournamentRoomFlowCoodinator)} does not have a host associated. Make sure to call roomFlowCoordinator.SetHost() when you present it.");
            }
            if (firstActivation)
            {
                showBackButton = true;
            }
            if (addedToHierarchy)
            {
                _ = ActivateAsync();
            }

            if (addedToHierarchy || screenSystemEnabling)
            {
                _pluginClient.ConnectedToServer += PluginClient_ConnectedToServer;
                _pluginClient.MatchCreated += PluginClient_MatchCreated;
                _pluginClient.MatchDeleted += PluginClient_MatchDeleted;
                _pluginClient.PlayerInfoUpdated += PluginClient_PlayerInfoUpdated;
                _pluginClient.FailedToConnectToServer += PluginClient_FailedToConnectToServer;
                _pluginClient.StartLevel += PluginClient_StartLevel;
                _pluginClient.LoadedSong += PluginClient_LoadedSong;
                _pluginClient.ServerDisconnected += PluginClient_ServerDisconnected;
                _pluginClient.MatchInfoUpdated += PluginClient_MatchInfoUpdated;
            }
        }

        private void PluginClient_ServerDisconnected()
        {
            UnityMainThreadTaskScheduler.Factory.StartNew(() =>
            {
                try
                {
                    _siraLog.Debug("Disconnected from the server.");
                    ServerDisconnected(_pluginClient);
                }
                catch (Exception e)
                {
                    _siraLog.Error(e);
                }
            });
        }

        private void PluginClient_LoadedSong(IBeatmapLevel level)
        {
            try
            {
                _siraLog.Debug($"Loaded a new song '{level.songName}'.");
                SongLoaded(_pluginClient, level);
            }
            catch (Exception e)
            {
                _siraLog.Error(e);
            }
        }

        private void PluginClient_StartLevel(StartLevelOptions level, MatchOptions match)
        {
            try
            {
                _siraLog.Debug($"Starting a level, '{level.Level.songName}'.");
                PlaySong(_pluginClient, level, match);
            }
            catch (Exception e)
            {
                _siraLog.Error(e);
            }
        }

        private void PluginClient_MatchCreated(Match match)
        {
            UnityMainThreadTaskScheduler.Factory.StartNew(() =>
            {
                try
                {
                    _siraLog.Debug($"A new match '{match.Guid}' has been created.");
                    _ongoingGameListView.AddMatches(match);
                    MatchCreated(_pluginClient, match);
                }
                catch (Exception e)
                {
                    _siraLog.Error(e);
                }
            });
        }

        private void PluginClient_MatchInfoUpdated(Match match)
        {
            UnityMainThreadTaskScheduler.Factory.StartNew(() =>
            {
                try
                {
                    _siraLog.Debug($"The match info for '{match.Guid}' has been updated.");
                    MatchUpdated(_pluginClient, match);
                }
                catch (Exception e)
                {
                    _siraLog.Error(e);
                }
            });
        }

        private void PluginClient_MatchDeleted(Match match)
        {
            UnityMainThreadTaskScheduler.Factory.StartNew(() =>
            {
                try
                {
                    _siraLog.Debug($"The match '{match.Guid}' has been deleted.");
                    _ongoingGameListView.RemoveMatch(match);
                    MatchDeleted(_pluginClient, match);
                }
                catch (Exception e)
                {
                    _siraLog.Error(e);
                }
            });
        }

        private void PluginClient_PlayerInfoUpdated(Player player)
        {
            UnityMainThreadTaskScheduler.Factory.StartNew(() =>
            {
                try
                {
                    _siraLog.Debug($"The player '{player.Name}' has been updated.");
                    PlayerUpdated(_pluginClient, player);
                }
                catch (Exception e)
                {
                    _siraLog.Error(e);
                }
            });
        }

        private void PluginClient_FailedToConnectToServer(ConnectResponse connect)
        {
            UnityMainThreadTaskScheduler.Factory.StartNew(() =>
            {
                try
                {
                    _siraLog.Warning($"Unable to connect to the server.");
                    SetLeftScreenViewController(null, ViewController.AnimationType.None);
                    SetRightScreenViewController(null, ViewController.AnimationType.None);
                    FailedToConnect(_pluginClient, connect);
                }
                catch (Exception e)
                {
                    _siraLog.Error(e);
                }
            });
        }

        private void PluginClient_ConnectedToServer(ConnectResponse connect)
        {
            if (_pluginClient.Self is Player player)
            {
                _siraLog.Debug($"Connected to the server.");
                _pluginClient.UpdatePlayer(player);
                player.ModList = PluginManager.EnabledPlugins.Select(x => x.Id).ToArray();
                UnityMainThreadTaskScheduler.Factory.StartNew(() =>
                {
                    try
                    {
                        _gameplaySetupViewController.Setup(false, true, true, false, PlayerSettingsPanelController.PlayerSettingsPanelLayout.Singleplayer);
                        SetLeftScreenViewController(_gameplaySetupViewController, ViewController.AnimationType.In);
                        SetRightScreenViewController(_ongoingGameListView, ViewController.AnimationType.In);
                        _ongoingGameListView.ClearMatches();
                        _ongoingGameListView.AddMatches(_pluginClient.State.Matches);
                        Connected(_pluginClient, player, connect);
                    }
                    catch (Exception e)
                    {
                        _siraLog.Error(e);
                    }
                });
            }
        }

        private async Task ActivateAsync()
        {
            if (_host is null)
            {
                throw new Exception($"The {nameof(TournamentRoomFlowCoodinator)} does not have a host associated. Make sure to call roomFlowCoordinator.SetHost() when you present it.");
            }

            try
            {
                await _pluginClient.Login(_host.Address, _host.Port);
            }
            catch (Exception e)
            {
                _siraLog.Error(e);
            }
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            _pluginClient.Logout();

            if (removedFromHierarchy || screenSystemDisabling)
            {
                _pluginClient.ConnectedToServer -= PluginClient_ConnectedToServer;
                _pluginClient.MatchCreated -= PluginClient_MatchCreated;
                _pluginClient.MatchDeleted -= PluginClient_MatchDeleted;
                _pluginClient.PlayerInfoUpdated -= PluginClient_PlayerInfoUpdated;
                _pluginClient.FailedToConnectToServer -= PluginClient_FailedToConnectToServer;
                _pluginClient.StartLevel -= PluginClient_StartLevel;
                _pluginClient.LoadedSong -= PluginClient_LoadedSong;
                _pluginClient.ServerDisconnected -= PluginClient_ServerDisconnected;
            }
            if (removedFromHierarchy && _ongoingGameListView.isInViewControllerHierarchy)
            {
                SetLeftScreenViewController(null, ViewController.AnimationType.None);
                SetRightScreenViewController(null, ViewController.AnimationType.None);
            }

            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            DismissRequested?.Invoke();
        }

        protected internal void SendDismissEvent()
        {
            DismissRequested?.Invoke();
        }

        protected virtual void PlaySong(PluginClient sender, StartLevelOptions level, MatchOptions match) { }
        protected abstract void Connected(PluginClient sender, Player player, ConnectResponse response);
        protected abstract void FailedToConnect(PluginClient sender, ConnectResponse response);
        protected virtual void SongLoaded(PluginClient sender, IBeatmapLevel level) { }
        protected virtual void PlayerUpdated(PluginClient sender, Player player) { }
        protected virtual void MatchCreated(PluginClient sender, Match match) { }
        protected virtual void MatchUpdated(PluginClient sender, Match match) { }
        protected virtual void MatchDeleted(PluginClient sender, Match match) { }
        protected virtual void ServerDisconnected(PluginClient sender) { }
    }
}