using System;
using System.Collections.Generic;

namespace Media
{
	public class PolyphonicSoundPlayer : IDisposable
	{
		List<SoundPlayer> freeSoundPlayers;
        List<SoundPlayer> inUseSoundPlayers;

		public PolyphonicSoundPlayer ()
		{
			freeSoundPlayers = new List<SoundPlayer> ();
            inUseSoundPlayers = new List<SoundPlayer> ();
		}

		public void Dispose ()
		{
			ReleasePlayers();
		}

		public SoundPlayer PlaySound (string filename, bool loop = false, bool playMultipleInstances = true, double volume = 1)
		{
            if ((! playMultipleInstances)
				&& IsInstanceAlreadyPlaying(filename))
            {
                return null;
            }

			SoundPlayer player;

			if (freeSoundPlayers.Count > 0)
			{
				player = freeSoundPlayers[0];
				freeSoundPlayers.RemoveAt(0);
			}
			else
			{
				player = new SoundPlayer ();
				player.PlaybackFinished += player_PlaybackFinished;
			}

			player.Play(filename, loop);
			player.Volume = volume;
            inUseSoundPlayers.Add(player);
			return player;
		}

        bool IsInstanceAlreadyPlaying(string filename)
        {
            foreach (SoundPlayer player in inUseSoundPlayers)
            {
                if (player.FileName == filename)
                {
                    return true;
                }
            }

            return false;
        }

		void player_PlaybackFinished (object sender, EventArgs args)
		{
			freeSoundPlayers.Add((SoundPlayer) sender);
            inUseSoundPlayers.Remove((SoundPlayer)sender);
		}

		void ReleasePlayers ()
		{
			foreach (SoundPlayer player in freeSoundPlayers)
			{
				player.Dispose();
			}
			freeSoundPlayers.Clear();

			foreach (SoundPlayer player in inUseSoundPlayers)
			{
				player.Dispose();
			}
			inUseSoundPlayers.Clear();
		}
	}
}