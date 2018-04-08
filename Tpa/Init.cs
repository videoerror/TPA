#region LICENCE
/*
Copyright 2017 - 2018 video_error

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

	http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
 */
#endregion

using System;
using System.Timers;
using System.Collections.Generic;

using fCraft;
using fCraft.Events;

namespace Tpa {

	internal sealed class Init : Plugin {

		public const string PlainName = "TPA";
		public const string PlainVersion = "0.1";
		public const string PlainAuthor = "video_error";
		public const string PlainDescription = "A LegendCraft plugin for the sending/receiving and then accepting/rejecting of teleport requests.";
		public const string PlainRegion = "en-US";
		public const string PlainNameAndVersion = PlainName + " - " + PlainVersion;
		public const string PlainNameAndVersionWithBrackets = "[" + PlainName + " - " + PlainVersion + "]";
		public const string StylizedName = "&ATPA";
		public const string StylizedVersion = "&E0&8.&A1";
		public const string StylizedAuthor = "&Fv&Ei&Bd&Ae&Do&8_&Cerror";
		public const string StylizedDescription = "&EA &5Legend&CCraft &Eplugin for the &Asending&8/&Areceiving &Eand then &Aaccepting&8/&Arejecting &Eof teleport requests&8.";
		public const string StylizedNameAndVersion = StylizedName + " &8- " + StylizedVersion;
		public const string StylizedNameAndVersionWithBrackets = "&8[" + StylizedNameAndVersion + "&8]";

		public string Name {
			get {
				return PlainName;
			}

			set {
			}
		}

		public string Version {
			get {
				return PlainVersion;
			}

			set {
			}
		}

		public string Author {
			get {
				return PlainAuthor;
			}
		}

		public string Description {
			get {
				return PlainDescription;
			}
		}

		public Init() {
		}

		private readonly string[] CommandNames = new string[] {
			"Tpa", "TpaOK", "TpaNo", "TpaInfo"
		};

		public void Initialize() {
			Logger.Log(LogType.ConsoleOutput,
					   PlainNameAndVersionWithBrackets + " Loading. . .");

			Server.ShutdownEnded += OnServerShutdownEnded;

			TeleportRequest.Expired += OnTeleportRequestExpired;

			CommandManager.RegisterCustomCommand(new CommandDescriptor {
				Name = CommandNames[0],

				Aliases = new string[] {
					"TeleportAsk"
				},

				Category = CommandCategory.Moderation,

				Permissions = new Permission[] {
					 Permission.TPA
				},

				Usage = "/Tpa <PlayerName>",

				Help = "Sends a teleportation request to a player.",

				NotRepeatable = true,

				Handler = TpaCommandHandler
			});

			CommandManager.RegisterCustomCommand(new CommandDescriptor {
				Name = CommandNames[1],

				Aliases = new string[] {
					"TeleportAskOK",
					"TpaOkay",
					"TeleportAskOkay"
				},

				Category = CommandCategory.Moderation,

				Permissions = new Permission[] {
					 Permission.TPA
				},

				Usage = "/TpaOk <PlayerName>(Optional)",

				Help = "Accepts teleport requests from either a player or the last received teleport request.",

				NotRepeatable = true,

				Handler = TpaOkCommandHandler
			});

			CommandManager.RegisterCustomCommand(new CommandDescriptor {
				Name = CommandNames[2],

				Aliases = new string[] {
					"TeleportAskNo",
					"TpaNope",
					"TeleportAskNope"
				},

				Category = CommandCategory.Moderation,

				Permissions = new Permission[] {
					 Permission.TPA
				},

				Usage = "/TpaNo <PlayerName>(Optional)",

				Help = "Rejects teleport requests from either a player or the last received teleport request.",

				NotRepeatable = true,

				Handler = TpaNoCommandHandler
			});

			CommandManager.RegisterCustomCommand(new CommandDescriptor {
				Name = CommandNames[3],

				Aliases = new string[] {
					"TpaInformation",
					"TeleportAskInformation",
					"TeleportAskInfo",
					"TpaI",
					"TpaAbout",
					"TeleportAskAbout",
					"TpaA"
				},

				Category = CommandCategory.Info,

				Permissions = new Permission[] {
					 Permission.TPA
				},

				Usage = "/TpaInfo",

				Help = "Displays information about the TPA plugin.",

				IsConsoleSafe = true,
				NotRepeatable = true,

				Handler = TpaInfoCommandHandler
			});

			Logger.Log(LogType.ConsoleOutput,
					   PlainNameAndVersionWithBrackets + " Loaded!");
		}

		public enum ExpirationReasonType {
			None,
			Timeout,
			TargetPlayerLeft,
			RequestingPlayerLeft
		}

		public class TeleportRequest : IDisposable {

			public bool IsExpired {
				get;

				private set;
			}

			public ExpirationReasonType TypeOfExpirationReasonType;

			public static event EventHandler Expired;

			public static double Timeout = 30000D;

			private Timer TimeoutTimer = new Timer(Timeout) {
				AutoReset = false
			};

			public Player TargetPlayer;
			public Player RequestingPlayer;

			public TeleportRequest(Player targetPlayer, Player requestingPlayer) {
				TargetPlayer = targetPlayer;
				RequestingPlayer = requestingPlayer;

				TimeoutTimer.Elapsed += TimeoutTimerElapsed;

				Player.Disconnected += OnPlayerDisconnected;
			}

			public void StartTimeout() {
				if(!IsExpired) {
					TimeoutTimer.Start();
				}
			}

			private void SendExpirationMessage(bool from, bool to,
											   string reason) {
				if(from) {
					TargetPlayer.TpaPluginMessage("Teleport request from " +
												  RequestingPlayer.ClassyName.EscapeSymbols() +
												  " expired because " + reason);
				}

				if(to) {
					RequestingPlayer.TpaPluginMessage("Teleport request to " +
													  TargetPlayer.ClassyName.EscapeSymbols() +
													  " expired because " + reason);
				}
			}

			private void TimeoutTimerElapsed(object source, ElapsedEventArgs elapsedEventArgs) {
				if(IsExpired) {
					return;
				}

				StopTimeout(true);

				SendExpirationMessage(true, true, "it timed out.");
			}

			private void RaiseExpiredEvent(ExpirationReasonType expirationReasonType) {
				EventHandler eventHandler = Expired;

				IsExpired = true;

				TypeOfExpirationReasonType = expirationReasonType;

				if(eventHandler != null) {
					eventHandler(this, EventArgs.Empty);
				}
			}

			public void StopTimeout(bool raiseExpiredEvent) {
				RaiseExpiredEvent(ExpirationReasonType.Timeout);

				TimeoutTimer.Stop();

				TimeoutTimer.Elapsed -= TimeoutTimerElapsed;
			}

			private void OnPlayerDisconnected(object sender,
											  PlayerDisconnectedEventArgs playerDisconnectedEventArgs) {
				if(IsExpired) {
					return;
				}

				Player player = playerDisconnectedEventArgs.Player;

				if(!playerDisconnectedEventArgs.IsFake) {
					if(player == TargetPlayer) {
						RaiseExpiredEvent(ExpirationReasonType.TargetPlayerLeft);

						SendExpirationMessage(false, true, "they left the server.");

						return;
					}

					if(player == RequestingPlayer) {
						RaiseExpiredEvent(ExpirationReasonType.RequestingPlayerLeft);

						SendExpirationMessage(true, false, "they left the server.");
					}
				}
			}

			public void Dispose() {
				StopTimeout(false);
			}

			~TeleportRequest() {
				Dispose();
			}
		}

		private static readonly object TpaRequestsLockObject = new object();

		public readonly Dictionary<Player, List<TeleportRequest>> TpaRequests =
			new Dictionary<Player, List<TeleportRequest>>();

		private void AddTeleportRequest(Player player, TeleportRequest teleportRequest) {
			lock(TpaRequestsLockObject) {
				List<TeleportRequest> teleportRequests = null;

				try {
					teleportRequests = TpaRequests[player];
				} catch {
					if(teleportRequests == null) {
						teleportRequests = new List<TeleportRequest>();
					}
				}

				teleportRequests.Add(teleportRequest);

				teleportRequest.StartTimeout();

				teleportRequests.TrimExcess();

				if(!TpaRequests.ContainsKey(player)) {
					TpaRequests.Add(player, teleportRequests);
				}
			}
		}

		private void RemoveTeleportRequest(Player player, TeleportRequest teleportRequest,
										   bool stopTimeout) {
			lock(TpaRequestsLockObject) {
				if(stopTimeout) {
					teleportRequest.StopTimeout(false);
				}

				List<TeleportRequest> teleportRequests = TpaRequests[player];

				teleportRequests.Remove(teleportRequest);

				teleportRequests.TrimExcess();
			}
		}

		private void OnTeleportRequestExpired(object sender, EventArgs eventArgs) {
			TeleportRequest teleportRequest = ((TeleportRequest)sender);

			RemoveTeleportRequest(teleportRequest.TargetPlayer, teleportRequest,
								  false);
		}

		private void SendNoPendingTeleportRequestsMessage(Player player, Player targetPlayer) {
			if(targetPlayer == null) {
				player.TpaPluginMessage("You have no pending teleport requests.");
			} else {
				player.TpaPluginMessage("You have no pending teleport requests from " +
										targetPlayer.ClassyName.EscapeSymbols() + ".");
			}
		}

		private enum TeleportErrorReasonType {
			None,
			Unknown,
			WorldFull,
			Blacklisted,
			RankTooLow,
			RankTooHigh
		}

		// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
		// Original code by Matvei Stefarov edited by video_error.
		private Tuple<bool, TeleportErrorReasonType> TryPlayerTeleportation(Player player, Player targetPlayer) {
			try {
				if(player == null || targetPlayer == null) {
					return new Tuple<bool, TeleportErrorReasonType>(false, TeleportErrorReasonType.None);
				}

				World targetWorld = targetPlayer.World;

				if(targetWorld == null) {
					return new Tuple<bool, TeleportErrorReasonType>(false, TeleportErrorReasonType.None);
				}

				if(targetWorld == player.World) {
					player.previousLocation = player.Position;

					player.previousWorld = null;

					player.TeleportTo(targetPlayer.Position);
				} else {
					switch(targetWorld.AccessSecurity.CheckDetailed(player.Info)) {
						case SecurityCheckResult.Allowed:
						case SecurityCheckResult.WhiteListed:
							if(targetWorld.IsFull) {
								return new Tuple<bool, TeleportErrorReasonType>(false,
																				TeleportErrorReasonType.WorldFull);
							}

							player.StopSpectating();

							player.previousLocation = player.Position;

							player.previousWorld = player.World;

							player.JoinWorld(targetWorld, WorldChangeReason.Tp, targetPlayer.Position);

							break;

						case SecurityCheckResult.BlackListed:
							return new Tuple<bool, TeleportErrorReasonType>(false,
																			TeleportErrorReasonType.Blacklisted);

						case SecurityCheckResult.RankTooLow:
							return new Tuple<bool, TeleportErrorReasonType>(false,
																			TeleportErrorReasonType.RankTooLow);

							// TODO: case PermissionType.RankTooHigh:
					}
				}

				return new Tuple<bool, TeleportErrorReasonType>(true, TeleportErrorReasonType.None);
			} catch {
				return new Tuple<bool, TeleportErrorReasonType>(false, TeleportErrorReasonType.Unknown);
			}
		}

		private void Tpa(Player player, string targetPlayerName, bool okOrNo) {
			lock(TpaRequestsLockObject) {
				bool targetPlayerNameEmpty =
					String.IsNullOrEmpty(targetPlayerName);

				List<TeleportRequest> teleportRequests = null;

				try {
					teleportRequests = TpaRequests[player];
				} catch {
				}

				TeleportRequest teleportRequest = null;

				Player targetPlayer = null;

				if(targetPlayerNameEmpty) {
					try {
						teleportRequest =
							teleportRequests[teleportRequests.Count - 1];

						targetPlayer = teleportRequest.RequestingPlayer;
					} catch {
					}
				} else {
					try {
						targetPlayer =
							Server.FindPlayerOrPrintMatches(player,
															targetPlayerName,
															false,
															true);

						if(targetPlayer == null) {
							return;
						}

						teleportRequest =
							teleportRequests.Find(teleportRequestEnumerator =>
												  teleportRequestEnumerator.RequestingPlayer ==
												  targetPlayer);
					} catch {
					}
				}

				if(TpaRequests.ContainsKey(player)) {
					if(teleportRequests == null || teleportRequests.Count <= 0) {
						TpaRequests.Remove(player);

						if(targetPlayerNameEmpty) {
							SendNoPendingTeleportRequestsMessage(player, null);
						} else {
							SendNoPendingTeleportRequestsMessage(player, targetPlayer);
						}

						return;
					}

					if(targetPlayerNameEmpty) {
						if(teleportRequest.IsExpired) {
							SendNoPendingTeleportRequestsMessage(player, null);

							RemoveTeleportRequest(player, teleportRequest, false);

							return;
						}
					} else {
						if(teleportRequest != null && teleportRequest.IsExpired) {
							RemoveTeleportRequest(player, teleportRequest, false);

							teleportRequest = null;
						}

						if(teleportRequest == null) {
							SendNoPendingTeleportRequestsMessage(player, targetPlayer);

							return;
						}
					}

					string targetPlayerEscapedClassyName = targetPlayer.ClassyName.EscapeSymbols();
					string playerEscapedClassyName = player.ClassyName.EscapeSymbols();

					if(okOrNo) {
						player.TpaPluginMessage("Teleport request from " +
												targetPlayerEscapedClassyName +
												" accepted.");
						targetPlayer.TpaPluginMessage("Teleport request to " +
													  playerEscapedClassyName +
													  " accepted.");
						player.TpaPluginMessage("Teleporting " +
												targetPlayerEscapedClassyName +
												" to you. . .");
						targetPlayer.TpaPluginMessage("Teleporting to " +
													  playerEscapedClassyName +
													  ". . .");

						Tuple<bool, TeleportErrorReasonType> playerTeleportationResult =
							TryPlayerTeleportation(targetPlayer, player);

						string playersErrorReasonMessage = "Teleporting " +
														   targetPlayerEscapedClassyName +
														   " to you failed because ";
						string targetPlayersErrorReasonMessage = "Teleporting to " +
																 playerEscapedClassyName +
																 " failed because ";
						string errorReasonMessage = null;

						if(!playerTeleportationResult.Item1) {
							string playerWorldClassyName =
								player.World.ClassyName.EscapeSymbols();

							switch(playerTeleportationResult.Item2) {
								case TeleportErrorReasonType.None:
								case TeleportErrorReasonType.Unknown:
									errorReasonMessage = "of an unknown error!";

									playersErrorReasonMessage += errorReasonMessage;
									targetPlayersErrorReasonMessage += errorReasonMessage;

									break;
								
								case TeleportErrorReasonType.WorldFull:
									errorReasonMessage = playerWorldClassyName + " is full!";

									playersErrorReasonMessage += errorReasonMessage;
									targetPlayersErrorReasonMessage += errorReasonMessage;

									break;

								case TeleportErrorReasonType.Blacklisted:
									errorReasonMessage = "blacklisted from " +
														 playerWorldClassyName + "!";

									playersErrorReasonMessage += "they're " +
																 errorReasonMessage;
									targetPlayersErrorReasonMessage += "you're " +
																	   errorReasonMessage;

									break;
								
								case TeleportErrorReasonType.RankTooLow:
									errorReasonMessage = "rank is too low for " +
														 playerWorldClassyName + "!";

									playersErrorReasonMessage += "their " +
																 errorReasonMessage;
									targetPlayersErrorReasonMessage += "your " +
																	   errorReasonMessage;

									break;
								
								case TeleportErrorReasonType.RankTooHigh:
									errorReasonMessage = "rank is too high for " +
														 playerWorldClassyName + "!";

									playersErrorReasonMessage += "their " +
																 errorReasonMessage;
									targetPlayersErrorReasonMessage += "your " +
																	   errorReasonMessage;

									break;
								
								default:
									goto case TeleportErrorReasonType.Unknown;
							}

							player.TpaPluginMessage(playersErrorReasonMessage);
							targetPlayer.TpaPluginMessage(targetPlayersErrorReasonMessage);

							RemoveTeleportRequest(player, teleportRequest, true);

							return;
						}

						player.TpaPluginMessage("Teleported " +
												targetPlayerEscapedClassyName +
												" to you!");
						targetPlayer.TpaPluginMessage("Teleported to " +
													  playerEscapedClassyName +
													  "!");
					}

					RemoveTeleportRequest(player, teleportRequest, true);

					if(!okOrNo) {
						player.TpaPluginMessage("Teleport request from " +
												targetPlayerEscapedClassyName +
												" rejected.");
						targetPlayer.TpaPluginMessage("Teleport request to " +
													  playerEscapedClassyName +
													  " rejected.");
					}
				} else {
					if(targetPlayerNameEmpty) {
						SendNoPendingTeleportRequestsMessage(player, null);
					} else {
						SendNoPendingTeleportRequestsMessage(player, targetPlayer);
					}
				}
			}
		}

		public void TpaCommandHandler(Player player, Command command) {
			string targetPlayerName = command.Next();

			if(String.IsNullOrEmpty(targetPlayerName)) {
				command.Descriptor.PrintUsage(player);

				return;
			}

			Player targetPlayer = Server.FindPlayerOrPrintMatches(player,
																  targetPlayerName,
																  false,
																  true);

			bool previousTeleportRequestExists = false;

			try {
				lock(TpaRequestsLockObject) {
					previousTeleportRequestExists =
						TpaRequests[targetPlayer].Exists(teleportRequestEnumerator =>
														 teleportRequestEnumerator.RequestingPlayer ==
														 player);
				}
			} catch {
			}

			if(targetPlayer == null) {
				return;
			} else if(player.Name == targetPlayer.Name) {
				player.TpaPluginMessage("Teleport requests cannot be sent to yourself.");

				return;
			} else if(previousTeleportRequestExists) {
				player.TpaPluginMessage("Another teleport request cannot be sent until the last one has expired after &A30 seconds " +
										"&Eor when either the sender or the receiver has left the server.");

				return;
			}

			TeleportRequest teleportRequest = new TeleportRequest(targetPlayer, player);

			AddTeleportRequest(targetPlayer, teleportRequest);

			player.TpaPluginMessage("Teleport request sent to " +
									targetPlayer.ClassyName.EscapeSymbols() + "!");
			targetPlayer.TpaPluginMessage("Teleport request received from " +
										  player.ClassyName.EscapeSymbols() +
										  ". To accept, type: /&A" + CommandNames[1] + ". " +
										  "To reject, type: /&A" + CommandNames[2] + ". " +
										  "This teleport request will timeout in &A30 seconds.");
		}

		public void TpaOkCommandHandler(Player player, Command command) {
			Tpa(player, command.Next(), true);
		}

		public void TpaNoCommandHandler(Player player, Command command) {
			Tpa(player, command.Next(), false);
		}

		private void TpaInfoCommandHandler(Player player, Command command) {
			player.TpaPluginMessage("/// " + StylizedNameAndVersion + " Info \\\\\\\n" +
									"Commands: /&A" + String.Join(", /&A", CommandNames) + "\n" +
									"Author: " + StylizedAuthor + "\n" +
									"Description: " + StylizedDescription);
		}

		private void OnServerShutdownEnded(object sender, ShutdownEventArgs shutdownEventArgs) {
			TeleportRequest.Expired -= OnTeleportRequestExpired;

			Server.ShutdownEnded -= OnServerShutdownEnded;
		}
	}
}
