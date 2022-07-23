using System;
using System.Runtime.InteropServices;

namespace LoudPizza.Core
{
    // Voice group operations
    public unsafe partial class SoLoud
    {
        /// <summary>
        /// Create a voice group. Returns 0 if unable (out of voice groups / out of memory).
        /// </summary>
        public Handle createVoiceGroup()
        {
            lock (mAudioThreadMutex)
            {
                int index;
                {
                    // Check if there's any deleted voice groups and re-use if found
                    Handle[][] voiceGroups = mVoiceGroup;
                    for (int i = 0; i < voiceGroups.Length; i++)
                    {
                        if (voiceGroups[i].Length == 0)
                        {
                            Handle[] groupa = new Handle[16];
                            if (groupa == null)
                            {
                                return default;
                            }
                            mVoiceGroup[i] = groupa;
                            return new Handle(0xfffff000 | (uint)i);
                        }
                    }
                    if (voiceGroups.Length == 4096)
                    {
                        return default;
                    }

                    Handle[][] vg = new Handle[Math.Max(4, voiceGroups.Length * 2)][];
                    if (vg == null)
                    {
                        return default;
                    }
                    voiceGroups.CopyTo(vg.AsSpan());
                    for (int i = voiceGroups.Length; i < vg.Length; i++)
                    {
                        vg[i] = Array.Empty<Handle>();
                    }

                    mVoiceGroup = vg;
                    index = voiceGroups.Length;
                }

                Handle[] groupb = new Handle[16];
                if (groupb == null)
                {
                    return default;
                }
                mVoiceGroup[index] = groupb;
                return new Handle(0xfffff000 | (uint)index);
            }
        }

        /// <summary>
        /// Destroy a voice group.
        /// </summary>
        public SoLoudStatus destroyVoiceGroup(Handle aVoiceGroupHandle)
        {
            if (!isVoiceGroup(aVoiceGroupHandle))
                return SoLoudStatus.InvalidParameter;

            int c = (int)(aVoiceGroupHandle.Value & 0xfff);

            lock (mAudioThreadMutex)
            {
                //delete[] mVoiceGroup[c];
                mVoiceGroup[c] = Array.Empty<Handle>();
                return SoLoudStatus.Ok;
            }
        }

        /// <summary>
        /// Add a voice handle to a voice group.
        /// </summary>
        public SoLoudStatus addVoiceToGroup(Handle aVoiceGroupHandle, Handle aVoiceHandle)
        {
            if (!isVoiceGroup(aVoiceGroupHandle))
                return SoLoudStatus.InvalidParameter;

            // Don't consider adding invalid voice handles as an error, since the voice may just have ended.
            if (!isValidVoiceHandle(aVoiceHandle))
                return SoLoudStatus.Ok;

            trimVoiceGroup_internal(aVoiceGroupHandle);

            int c = (int)(aVoiceGroupHandle.Value & 0xfff);

            lock (mAudioThreadMutex)
            {
                Handle[] group = mVoiceGroup[c];
                for (int i = 0; i < group.Length; i++)
                {
                    if (group[i] == aVoiceHandle)
                    {
                        return SoLoudStatus.Ok; // already there
                    }

                    if (group[i] == default)
                    {
                        group[i] = aVoiceHandle;
                        return SoLoudStatus.Ok;
                    }
                }

                // Full group, allocate more memory
                int newLength = Math.Max(16, group.Length * 2);
                Handle[] newGroup = new Handle[newLength];
                if (newGroup == null)
                {
                    return SoLoudStatus.OutOfMemory;
                }

                group.CopyTo(newGroup.AsSpan());
                newGroup[group.Length] = aVoiceHandle;

                mVoiceGroup[c] = newGroup;
                return SoLoudStatus.Ok;
            }
        }

        /// <summary>
        /// Get whether the given handle is a valid voice group.
        /// </summary>
        public bool isVoiceGroup(Handle aVoiceGroupHandle)
        {
            if ((aVoiceGroupHandle.Value & 0xfffff000) != 0xfffff000)
                return false;

            int c = (int)(aVoiceGroupHandle.Value & 0xfff);

            lock (mAudioThreadMutex)
            {
                Handle[][] voiceGroups = mVoiceGroup;
                if (c >= voiceGroups.Length)
                    return false;

                bool res = voiceGroups[c].Length != 0;
                return res;
            }
        }

        /// <summary>
        /// Get whether the given voice group is empty.
        /// </summary>
        public bool isVoiceGroupEmpty(Handle aVoiceGroupHandle)
        {
            // If not a voice group, yeah, we're empty alright..
            if (!isVoiceGroup(aVoiceGroupHandle))
                return true;

            trimVoiceGroup_internal(aVoiceGroupHandle);
            int c = (int)(aVoiceGroupHandle.Value & 0xfff);

            lock (mAudioThreadMutex)
            {
                bool res = mVoiceGroup[c].Length != 0;
                return res;
            }
        }

        /// <summary>
        /// Remove all non-active voices from group.
        /// </summary>
        internal void trimVoiceGroup_internal(Handle aVoiceGroupHandle)
        {
            if (!isVoiceGroup(aVoiceGroupHandle))
                return;

            int c = (int)(aVoiceGroupHandle.Value & 0xfff);

            lock (mAudioThreadMutex)
            {
                Handle[] group = mVoiceGroup[c];

                for (int i = 0; i < group.Length; i++)
                {
                    // If we hit a voice in the group that's not set, we're done
                    if (group[i] == default)
                    {
                        return;
                    }

                    while (!isValidVoiceHandle(group[i]))
                    {
                        // current index is an invalid handle, move all following handles backwards
                        for (int j = i; j < group.Length - 1; j++)
                        {
                            group[j] = group[j + 1];
                            // not a full group, we can stop copying
                            if (group[j] == default)
                                break;
                        }

                        // did we end up with an empty group? we're done then
                        if (group[i] == default)
                        {
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets a span to the zero-terminated array of voice handles in a voice group.
        /// </summary>
        internal ReadOnlySpan<Handle> VoiceGroupHandleToSpan(ref Handle aVoiceGroupHandle)
        {
            if ((aVoiceGroupHandle.Value & 0xfffff000) != 0xfffff000)
                return MemoryMarshal.CreateReadOnlySpan(ref aVoiceGroupHandle, 1);

            int c = (int)(aVoiceGroupHandle.Value & 0xfff);

            Handle[][] voiceGroups = mVoiceGroup;
            if (c >= voiceGroups.Length)
                return MemoryMarshal.CreateReadOnlySpan(ref aVoiceGroupHandle, 1);

            Handle[] group = voiceGroups[c];
            return group;
        }
    }
}
