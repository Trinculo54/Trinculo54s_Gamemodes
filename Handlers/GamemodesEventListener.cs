using Impostor.Api.Events;
using Impostor.Api.Events.Announcements;
using Impostor.Api.Events.Player;
using Impostor.Api.Games;
using Impostor.Api.Innersloth;
using Impostor.Api.Innersloth.Customization;
using Impostor.Api.Net;
using Impostor.Api.Net.Inner.Objects;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gamemodes
{
    public class GamemodesEventListener : IEventListener
    {
        // private readonly string help =  "<align=center>\nInfected are green, Survivors are blue. The infected can infect the survivors by standing near them.\n Objective of the Infected: Infect Everyone.\n Objective of the Survivors: finish all their tasks.\nㅤ";
        private readonly List<IGame> Infectionoff = new();
        private readonly List<IGame> HNSOff = new();
        private readonly List<IGame> FTAGOff = new();
        private readonly Dictionary<IGame, InfectionInfos> InfectionInfos = new();
        private readonly Dictionary<IGame, HNSInfo> HNSInfo = new();
        private readonly Dictionary<IGame, FTAGInfo> FTAGInfos = new();
        private readonly ILogger<GamemodesPlugin> _logger;
        private const float Infectionradius = 0.2f;
        private const float Ftagradius = 0.2f;
        private const float HNSradius = 0.5f;
        private const int Id = 50;

        public GamemodesEventListener(ILogger<GamemodesPlugin> logger)
        {
            _logger = logger;
        }

        [EventListener]
        public async ValueTask OnGameStarting(IGameStartingEvent e)
        {
            if (!Infectionoff.Contains(e.Game))
            {
                e.Game.Options.NumEmergencyMeetings = 0;
                e.Game.Options.CrewLightMod = 5f;
                await e.Game.SyncSettingsAsync();
            }
            if (!HNSOff.Contains(e.Game))
            {
                e.Game.Options.NumEmergencyMeetings = 0;
                await e.Game.SyncSettingsAsync();
            }
            if (!FTAGOff.Contains(e.Game))
            {
                e.Game.Options.NumEmergencyMeetings = 0;
                await e.Game.SyncSettingsAsync();
            }
        }

        [EventListener]
        public void OnAnnouncementRequestEvent(IAnnouncementRequestEvent e)
        {
            if (e.Id == Id)
            {
                e.Response.UseCached = true;
            }
            else
            {
                e.Response.Announcement = new Announcement(Id, "TEST");
            }
        }

        [EventListener]
        public async ValueTask OnGameStarted(IGameStartedEvent e)
        {
            if (!HNSOff.Contains(e.Game))
            {
                List<IClientPlayer> impostors = new();
                foreach (var player in e.Game.Players)
                {
                    if (player.Character.PlayerInfo.IsImpostor)
                    {
                        await player.Character.SetColorAsync(ColorType.Red);
                        await player.Character.SetHatAsync(HatType.NoHat);
                        await player.Character.SetPetAsync(PetType.NoPet);
                        await player.Character.SetSkinAsync(SkinType.None);
                        await player.Character.SetNameAsync("Seeker");
                        impostors.Add(player);
                    }
                    else
                    {
                        await player.Character.SetColorAsync(ColorType.Blue);
                        await player.Character.SetHatAsync(HatType.NoHat);
                        await player.Character.SetSkinAsync(SkinType.None);
                        await player.Character.SetPetAsync(PetType.NoPet);
                        await player.Character.SetNameAsync("Hider");
                    }
                }
                HNSInfo.Add(e.Game, new HNSInfo(impostors));
            }

            if (!FTAGOff.Contains(e.Game))
            {
                List<IClientPlayer> impostors = new();
                ConcurrentDictionary<IClientPlayer, Vector2> frozen = new();
                foreach (var player in e.Game.Players)
                {
                    if (player.Character.PlayerInfo.IsImpostor)
                    {
                        await player.Character.SetColorAsync(ColorType.Red);
                        impostors.Add(player);
                    }
                    else
                    {
                        await player.Character.SetColorAsync(ColorType.Green);
                    }
                }
                FTAGInfos.Add(e.Game, new FTAGInfo(impostors, frozen));
            }

            if (!Infectionoff.Contains(e.Game))
            {
                List<IClientPlayer> impostors = new();
                foreach (var player in e.Game.Players)
                {
                    if (player.Character.PlayerInfo.IsImpostor)
                    {
                        await player.Character.SetColorAsync(ColorType.Green);
                        await player.Character.SetHatAsync(HatType.DumSticker);
                        await player.Character.SetPetAsync(PetType.NoPet);
                        await player.Character.SetSkinAsync(SkinType.None);
                        await player.Character.SetNameAsync("Infected");
                        impostors.Add(player);
                    }
                    else
                    {
                        await player.Character.SetColorAsync(ColorType.Blue);
                        await player.Character.SetHatAsync(HatType.Police);
                        await player.Character.SetSkinAsync(SkinType.Police);
                        await player.Character.SetPetAsync(PetType.NoPet);
                        await player.Character.SetNameAsync("Survivor");
                    }
                }
                InfectionInfos.Add(e.Game, new InfectionInfos(impostors));
            }
        }

        [EventListener]
        public async ValueTask OnPlayerMovement(IPlayerMovementEvent e)
        {
            if (HNSInfo.ContainsKey(e.Game))
            {
                List<IClientPlayer> impostors = HNSInfo[e.Game].impostors;

                //Gives an iterator of all non-frozen crewmates
                IEnumerable<IClientPlayer> crewmates = e.Game.Players.Except(impostors);

                foreach (var impostor in impostors)
                {
                    foreach (var crewmate in crewmates)
                    {
                        //Checks if near impostor
                        if (CheckIfCollidinghns(crewmate, impostor))
                        {
                            //Puts crewmate in list and makes him infected
                            await crewmate.Character.SetColorAsync(ColorType.Red);
                            await crewmate.Character.SetHatAsync(HatType.NoHat);
                            await crewmate.Character.SetSkinAsync(SkinType.None);
                            await crewmate.Character.SetNameAsync("Seeker");
                            await crewmate.Character.SetPetAsync(PetType.NoPet);
                            impostors.Add(crewmate);
                            var Options = new GameOptionsData();
                            Options.CrewLightMod = 0.2f;
                            Options.PlayerSpeedMod = 0.5f;
                            Options.NumCommonTasks = 0;
                            Options.NumLongTasks = 0;
                            Options.NumShortTasks = 0;
                            await crewmate.Game.SendSettingsToPlayerAsync(Options, crewmate.Character);
                        }
                        if (impostors.Count.Equals(e.Game.PlayerCount))
                        {
                            foreach (var player in e.Game.Players)
                            {
                                if (!player.Character.PlayerInfo.IsDead && player.Character.PlayerInfo.IsImpostor)
                                {
                                    await player.Character.MurderPlayerAsync(player.Character);
                                }
                            }
                        }
                    }
                }
            }

            if (InfectionInfos.ContainsKey(e.Game))
            {
                List<IClientPlayer> impostors = InfectionInfos[e.Game].impostors;

                //Gives an iterator of all non-frozen crewmates
                IEnumerable<IClientPlayer> crewmates = e.Game.Players.Except(impostors);

                foreach (var impostor in impostors)
                {
                    foreach (var crewmate in crewmates)
                    {
                        //Checks if near impostor
                        if (CheckIfCollidinginfection(crewmate, impostor))
                        {
                            //Puts crewmate in list and makes him infected
                            await crewmate.Character.SetColorAsync(ColorType.Green);
                            await crewmate.Character.SetHatAsync(HatType.DumSticker);
                            await crewmate.Character.SetSkinAsync(SkinType.None);
                            await crewmate.Character.SetNameAsync("Infected");
                            await crewmate.Character.SetPetAsync(PetType.NoPet);
                            impostors.Add(crewmate);
                            var Options = new GameOptionsData();
                            Options.CrewLightMod = 0.2f;
                            Options.PlayerSpeedMod = 0.5f;
                            Options.NumCommonTasks = 0;
                            Options.NumLongTasks = 0;
                            Options.NumShortTasks = 0;
                            await crewmate.Game.SendSettingsToPlayerAsync(Options, crewmate.Character);
                        }
                        if (impostors.Count.Equals(e.Game.PlayerCount))
                        {
                            foreach (var player in e.Game.Players)
                            {
                                if (!player.Character.PlayerInfo.IsDead && player.Character.PlayerInfo.IsImpostor)
                                {
                                    await player.Character.MurderPlayerAsync(player.Character);
                                }
                            }
                        }
                    }
                }
            }

            if (FTAGInfos.ContainsKey(e.Game))
            {
                List<IClientPlayer> impostors = FTAGInfos[e.Game].impostors;
                ConcurrentDictionary<IClientPlayer, Vector2> frozens = FTAGInfos[e.Game].frozens;

                //Gives an iterator of all non-frozen crewmates
                IEnumerable<IClientPlayer> crewmates = e.Game.Players.Except(impostors).Except(frozens.Keys);

                //All crewmates are frozen, starting impostor winning process
                if (!crewmates.Any())
                {
                    //Every non impostor gets killed
                    foreach (var nonImpostor in e.Game.Players.Except(impostors))
                    {
                        await nonImpostor.Character.MurderPlayerAsync(impostors[0].Character);
                    }
                    ClearDict(e);
                }


                foreach (var impostor in impostors)
                {
                    //I am not updating the list if an impostor leaves, so I'll leave this check here for now
                    if (impostor.Character != null)
                    {
                        foreach (var crewmate in crewmates)
                        {
                            //Checks if near impostor
                            if (CheckIfCollidinginfection(crewmate, impostor))
                            {
                                //Puts crewmate in list and makes him blue
                                frozens.TryAdd(crewmate, crewmate.Character.NetworkTransform.Position);
                                await crewmate.Character.SetColorAsync(ColorType.Blue);
                            }
                        }
                    }
                }

                foreach (var pair in frozens)
                {
                    IClientPlayer frozen = pair.Key;
                    Vector2 position = pair.Value;

                    //The frozen tries to move
                    if (frozen.Character.NetworkTransform.Position != position)
                    {
                        await frozen.Character.NetworkTransform.SnapToAsync(position);
                    }

                    foreach (var sun in crewmates)
                    {
                        if (sun != frozen && CheckIfCollidinginfection(sun, frozen))
                        {
                            await Unfreeze(frozen).ConfigureAwait(true);
                            frozens.Remove(frozen, out position);
                        }
                    }
                }
            }
        }


        private bool CheckIfCollidinginfection(IClientPlayer player1, IClientPlayer player2)
        {
            Vector2 crewmatePos = player1.Character.NetworkTransform.Position;
            Vector2 impostorPos = player2.Character.NetworkTransform.Position;
            float crewmateX = (float)Math.Round(crewmatePos.X, 1);
            float crewmateY = (float)Math.Round(crewmatePos.Y, 1);
            float impostorX = (float)Math.Round(impostorPos.X, 1);
            float impostorY = (float)Math.Round(impostorPos.Y, 1);
            return (crewmateX <= impostorX + Infectionradius && crewmateX >= impostorX - Infectionradius && crewmateY <= impostorY + Infectionradius && crewmateY >= impostorY - Infectionradius);
        }

        private bool CheckIfCollidinghns(IClientPlayer player1, IClientPlayer player2)
        {
            Vector2 crewmatePos = player1.Character.NetworkTransform.Position;
            Vector2 impostorPos = player2.Character.NetworkTransform.Position;
            float crewmateX = (float)Math.Round(crewmatePos.X, 1);
            float crewmateY = (float)Math.Round(crewmatePos.Y, 1);
            float impostorX = (float)Math.Round(impostorPos.X, 1);
            float impostorY = (float)Math.Round(impostorPos.Y, 1);
            return (crewmateX <= impostorX + Infectionradius && crewmateX >= impostorX - Infectionradius && crewmateY <= impostorY + Infectionradius && crewmateY >= impostorY - Infectionradius);
        }

        private async ValueTask Unfreeze(IClientPlayer frozen)
        {
            Thread.Sleep(2500);
            await frozen.Character.SetColorAsync(ColorType.Green);
        }

        [EventListener]
        public void OnGameEnded(IGameEndedEvent e)
        {
            ClearDict(e);
        }

        [EventListener]
        public void OnLobbyCreate(IPlayerSpawnedEvent e)
        {
            if (e.ClientPlayer.IsHost)
            {
                Infectionoff.Add(e.Game);
                HNSOff.Add(e.Game);
                FTAGOff.Add(e.Game);
                ClearDict(e);
            }
        }

        private void ClearDict(IGameEvent e)
        {
            if (FTAGInfos.ContainsKey(e.Game))
            {
                FTAGInfos[e.Game].impostors.Clear();
                FTAGInfos.Remove(e.Game);
            }
            if (HNSInfo.ContainsKey(e.Game))
            {
                HNSInfo[e.Game].impostors.Clear();
                HNSInfo.Remove(e.Game);
            }
            if (InfectionInfos.ContainsKey(e.Game))
            {
                InfectionInfos[e.Game].impostors.Clear();
                InfectionInfos.Remove(e.Game);
            }
        }

        /*[EventListener]
        public ValueTask OnPlayerDeath(IPlayerMurderEvent e)
        {
            if (CodeAndInfos.ContainsKey(e.Game))
            {
                if (e.Victim.PlayerInfo.IsImpostor)
                {
                    return new ValueTask();
                }
                else
                {
                    e.ClientPlayer.Client.DisconnectAsync(DisconnectReason.Custom, "Kicked for reason:\n Attempted to kill player");
                }
            }
            return new ValueTask();
        }*/

        [EventListener]
        public async ValueTask OnPlayerChat(IPlayerChatEvent e)
        {
            if (e.Game.GameState == GameStates.NotStarted && e.Message.StartsWith("/gamemode "))
            {
                switch (e.Message.ToLowerInvariant()[10..])
                {
                    case "infection":
                        if (e.ClientPlayer.IsHost)
                        {
                            if (Infectionoff.Contains(e.Game))
                            {

                                Infectionoff.Remove(e.Game);
                                FTAGOff.Add(e.Game);
                                HNSOff.Add(e.Game);
                                await ServerSendChatAsync("\nInfection has been activated for this game.</align>\n", e.ClientPlayer.Character);
                            }
                            else
                            {
                                await ServerSendChatAsync("<align=center>\nInfection was already active.</align>\nㅤ", e.ClientPlayer.Character);
                            }
                        }
                        else
                        {
                            await ServerSendChatAsync("<align=center>\nYou can't change the gamemode because you aren't the host.</align>\nㅤ", e.ClientPlayer.Character, true);
                        }
                        break;
                    case "hns":
                        if (e.ClientPlayer.IsHost)
                        {
                            if (HNSOff.Contains(e.Game))
                            {
                                HNSOff.Remove(e.Game);
                                FTAGOff.Add(e.Game);
                                Infectionoff.Add(e.Game);
                                await ServerSendChatAsync("<align=center>\nHide and Seek has been activated for this game.</align>\nㅤ", e.ClientPlayer.Character);
                            }
                            else
                            {
                                Infectionoff.Add(e.Game);
                                FTAGOff.Add(e.Game);
                                await ServerSendChatAsync("<align=center>\nHide and Seek has been activated for this game.</align>\nㅤ", e.ClientPlayer.Character);
                            }
                        }
                        else
                        {
                            await ServerSendChatAsync("<align=center>\nYou can't change the gamemode because you aren't the host.</align>\nㅤ", e.ClientPlayer.Character, true);
                        }
                        break;
                    case "ftag":
                        if (e.ClientPlayer.IsHost)
                        {
                            if (FTAGOff.Contains(e.Game))
                            {
                                FTAGOff.Remove(e.Game);
                                Infectionoff.Remove(e.Game);
                                HNSOff.Remove(e.Game);
                                await ServerSendChatAsync("<align=center>\nFreeze Tag has been activated for this game.</align>\nㅤ", e.ClientPlayer.Character);
                            }
                            else
                            {
                                await ServerSendChatAsync("<align=center>\nFreeze Tag was already active.</align>\nㅤ", e.ClientPlayer.Character);
                            }
                        }
                        else
                        {
                            await ServerSendChatAsync("<align=center>\nYou can't change the gamemode because you aren't the host.</align>\nㅤ", e.ClientPlayer.Character, true);
                        }
                        break;
                    default:
                        await ServerSendChatAsync($"<align=center>\n{e.Message[10..]} is not a valid Gamemode\n Use /gamemodes for a list of our gamemodes</align>\nㅤ", e.ClientPlayer.Character, true);
                        break;
                }
            }
            if (e.Game.GameState == GameStates.NotStarted && e.Message == "/gamemodes")
            {
                await ServerSendChatAsync("\n<align=center>You can toggle the gamemode by doing '/infection on/off', and see the roles with '/infection help'</align>\nㅤ", e.ClientPlayer.Character, true);
            }
            if (e.Message == "TEST")
            {
                byte[] bytes = { 0x5c, 0x6e, 0x3c, 0x63, 0x6f, 0x6c, 0x6f, 0x72, 0x3d, 0x23, 0x23, 0x63, 0x33, 0x66, 0x66, 0x30, 0x30, 0x3e, 0x54, 0x45, 0x53, 0x54, 0x3c, 0x2f, 0x63, 0x6f, 0x6c, 0x6f, 0x72, 0x3e, 0x5b, 0x41, 0x41, 0x30, 0x30, 0x30, 0x30, 0x46, 0x46, 0x5d, 0x54, 0x65, 0x73, 0x74 };
                string text = Encoding.UTF8.GetString(bytes);
                await ServerSendChatAsync("\n<color=##c3ff00>TEST</color>[AA0000FF]Test", e.ClientPlayer.Character);
            }
        }

        private async ValueTask ServerSendChatAsync(string text, IInnerPlayerControl player, bool toPlayer = false)
        {
            string playername = player.PlayerInfo.PlayerName;
            byte playercolor = (byte)player.PlayerInfo.Color;
            byte playerhat = (byte)player.PlayerInfo.Hat;
            byte playerskin = (byte)player.PlayerInfo.Skin;
            await player.SetNameAsync("<align=center><color=#0000FF><size=140%>Trinculo54.tech</size></color></align>");
            await player.SetColorAsync(ColorType.Blue);
            await player.SetHatAsync(HatType.NoHat);
            if (toPlayer)
            {
                await player.SendChatToPlayerAsync(text);
            }
            else
            {
                await player.SendChatAsync(text);
            }
            await player.SetColorAsync((ColorType)playercolor);
            await player.SetNameAsync(playername);
            await player.SetHatAsync((HatType)playerhat);
            await player.SetSkinAsync((SkinType)playerskin);
        }
    }
}