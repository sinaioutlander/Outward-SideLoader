using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace SideLoader
{
    /// <summary>Helper class used to manage and replace Audio and Music.</summary>
    public class CustomAudio
    {
        /// <summary>The GlobalAudioManager Instance reference (since its not public)</summary>
        public static GlobalAudioManager GAMInstance => References.GLOBALAUDIOMANAGER;

        /// <summary>
        /// List of AudioClips which have been replaced.
        /// </summary>
        public static readonly List<GlobalAudioManager.Sounds> ReplacedClips = new List<GlobalAudioManager.Sounds>();

        /// <summary>
        /// Load an Audio Clip from a given file path (on disk), and optionally put it in the provided SL Pack.
        /// </summary>
        /// <param name="filePath">The file path (must be a .WAV file) to load from</param>
        /// <param name="pack">Optional SL Pack to put the audio clip inside.</param>
        /// <param name="onClipLoaded">Event invoked when the AudioClip has finished loading, if successful.</param>
        /// <returns>The loaded audio clip, if successful.</returns>
        public static void LoadAudioClip(string filePath, SLPack pack = null, Action<AudioClip> onClipLoaded = null)
        {
            if (!File.Exists(filePath))
                return;

            //var data = File.ReadAllBytes(filePath);
            //return LoadAudioClip(data, Path.GetFileNameWithoutExtension(filePath), pack);

            SLPlugin.Instance.StartCoroutine(LoadAudioFromFileCoroutine(filePath, pack, onClipLoaded));
        }

        private static IEnumerator LoadAudioFromFileCoroutine(string filePath, SLPack pack, Action<AudioClip> onClipLoaded)
        {
            var name = Path.GetFileNameWithoutExtension(filePath);
            var request = UnityWebRequestMultimedia.GetAudioClip($@"file://{Path.GetFullPath(filePath)}", AudioType.WAV);

            yield return request.SendWebRequest();

            while (!request.isDone)
                yield return null;

            if (!string.IsNullOrEmpty(request.error))
            {
                SL.LogWarning(request.error);
                yield break;
            }

            SL.Log($"Loaded audio clip: {Path.GetFileName(filePath)}");

            var clip = DownloadHandlerAudioClip.GetContent(request);
            FinalizeAudioClip(clip, name, pack);

            onClipLoaded?.Invoke(clip);
        }

        /// <summary>
        /// Load an Audio Clip from a given byte array, and optionally put it in the provided SL Pack.<br/><br/>
        /// WARNING: AudioClips loaded from byte arrays are currently unreliable and may be glitched, use at own risk!
        /// </summary>
        /// <param name="data">The byte[] array from <see cref="File.ReadAllBytes(string)"/> on the wav file path.</param>
        /// <param name="name">The name to give to the audio clip.</param>
        /// <param name="pack">Optional SL Pack to put the audio clip inside.</param>
        /// <returns>The loaded audio clip, if successful.</returns>
        public static AudioClip LoadAudioClip(byte[] data, string name, SLPack pack = null)
        {
            SL.LogWarning("WARNING: AudioClips loaded from embedded .zip archives are currently unreliable and may be glitched, use at own risk!");

            try
            {
                var clip = ConvertByteArrayToAudioClip(data, name);

                if (!clip)
                    return null;

                return FinalizeAudioClip(clip, name, pack);
            }
            catch (Exception ex)
            {
                SL.LogWarning("Exception loading AudioClip!");
                SL.LogInnerException(ex);
                return null;
            }
        }

        // Finalize the clip (name / SLPack), and try to replace global game audio if one has the same name.
        internal static AudioClip FinalizeAudioClip(AudioClip clip, string name, SLPack pack = null)
        {
            clip.name = name;

            if (pack != null)
            {
                if (pack.AudioClips.ContainsKey(name))
                {
                    SL.LogWarning("Replacing clip '" + name + "' in pack '" + pack.Name + "'");

                    if (pack.AudioClips[name])
                        GameObject.Destroy(pack.AudioClips[name]);

                    pack.AudioClips.Remove(name);
                }

                pack.AudioClips.Add(name, clip);
            }

            if (Enum.TryParse(name, out GlobalAudioManager.Sounds sound))
                ReplaceAudio(sound, clip);

            return clip;
        }

        #region REPLACING GAME AUDIO

        /// <summary>Replace a global sound with the provided AudioClip.</summary>
        public static void ReplaceAudio(GlobalAudioManager.Sounds sound, AudioClip clip)
        {
            if (!GAMInstance)
            {
                SL.LogWarning("Cannot find GlobalAudioManager Instance!");
                return;
            }

            if (ReplacedClips.Contains(sound))
                SL.Log($"The Sound clip '{sound}' has already been replaced, replacing again...");

            try
            {
                DoReplaceClip(sound, clip);
            }
            catch (Exception e)
            {
                SL.LogError($"Exception replacing clip '{sound}'.\r\nMessage: {e.Message}\r\nStack: {e.StackTrace}");
            }
        }

        private static void DoReplaceClip(GlobalAudioManager.Sounds _sound, AudioClip _newClip)
        {
            if (!_newClip)
            {
                SL.LogWarning($"The replacement clip for '{_sound}' is null");
                return;
            }

            var path = GAMInstance.GetPrefabPath(_sound);
            var resource = Resources.Load<GameObject>($"_Sounds/{path}");
            var component = resource.GetComponent<AudioSource>();
            component.clip = _newClip;

            resource.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            if (!ReplacedClips.Contains(_sound))
                ReplacedClips.Add(_sound);

            SL.Log("Replaced " + _sound + " AudioSource with custom clip!");
        }

        #endregion

        #region WAV BYTE[] TO AUDIOCLIP CONVERSION

        // Not working properly at the moment. Only works for one specific format of PCM 16-bit int, not exactly sure which.

        /// <summary>
        /// Convert a byte[] array from an uncompressed WAV file (PCM or WaveFormatExtensable) into an AudioClip.
        /// </summary>
        /// <param name="sourceData">The byte[] array from the WAV file, eg. from <see cref="File.ReadAllBytes(string)"/> on the source file.</param>
        /// <param name="name">The name to give to the resulting AudioClip.</param>
        /// <returns>The AudioClip, if successful.</returns>
        public static AudioClip ConvertByteArrayToAudioClip(byte[] sourceData, string name = "wav")
        {
            try
            {
                // Get the bit-depth
                ushort bitDepth = BitConverter.ToUInt16(sourceData, 34);

                // calculate how much padding to add to the header offset
                int headerPad = BitConverter.ToInt32(sourceData, 16);

                // Calculate the offset and data size
                int headerOffset = 16 + 4 + headerPad + 4;
                int dataLength = BitConverter.ToInt32(sourceData, headerOffset);

                // add additional header offset
                headerOffset += sizeof(int);

                // prepare block sizes and output
                int blockSize = bitDepth / 8;
                int outputLength = dataLength / blockSize;

                float[] floatData = new float[outputLength];

                // get the max value which the float data will use to divide by
                int maxValue;
                switch (bitDepth)
                {
                    case 8: maxValue = sbyte.MaxValue; break;
                    case 16: maxValue = Int16.MaxValue; break;
                    case 24:
                    case 32: maxValue = Int32.MaxValue; break;
                    default: throw new NotImplementedException($"Bit-depth '{bitDepth}' is not supported.");
                }

                int offset;
                int i = 0;

                switch (bitDepth)
                {
                    case 8:
                        // 8 bit is easiest, just a pretty standard conversion.
                        while (i < outputLength)
                        {
                            floatData[i] = (float)sourceData[i] / maxValue;
                            ++i;
                        }
                        break;

                    case 16:
                        // for 16-bit, we just use BitConverter.ToInt16 with a calculated offset.
                        while (i < outputLength)
                        {
                            offset = i * blockSize + headerOffset;
                            floatData[i] = (float)BitConverter.ToInt16(sourceData, offset) / maxValue;
                            ++i;
                        }
                        break;

                    case 24:
                        // 24-bit is the most unique one, it uses a 4-byte block but 1 byte is just padding offset.
                        byte[] block = new byte[sizeof(Int32)];
                        while (i < outputLength)
                        {
                            offset = i * blockSize + headerOffset;
                            Buffer.BlockCopy(sourceData, offset, block, 1, blockSize);
                            floatData[i] = (float)BitConverter.ToInt32(block, 0) / maxValue;
                            ++i;
                        }
                        break;

                    case 32:
                        // 32-bit is basically the same as 16-bit, but uses BitConverter.ToInt32.
                        while (i < outputLength)
                        {
                            offset = i * blockSize + headerOffset;
                            floatData[i] = (float)BitConverter.ToInt32(sourceData, offset) / maxValue;
                            ++i;
                        }
                        break;
                }

                // get the channel and sampleRate information, required by AudioClip.Create()
                ushort channels = BitConverter.ToUInt16(sourceData, 22);
                int sampleRate = BitConverter.ToInt32(sourceData, 24);

                // finally create the actual AudioClip, set the data, and return.
                AudioClip audioClip = AudioClip.Create(name, floatData.Length, (int)channels, sampleRate, false);
                audioClip.SetData(floatData, 0);

                return audioClip;
            }
            catch (Exception ex)
            {
                SL.LogWarning("Exception converting byte[] array to AudioClip.");
                SL.Log(ex.ToString());

                return null;
            }
        }

        #endregion
    }
}
