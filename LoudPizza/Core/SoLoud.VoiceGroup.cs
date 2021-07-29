using System;

namespace LoudPizza
{
    // Voice group operations
    public unsafe partial class SoLoud
    {
        // Create a voice group. Returns 0 if unable (out of voice groups / out of memory)
        public Handle createVoiceGroup()
        {
            lockAudioMutex_internal();

            uint i;
            // Check if there's any deleted voice groups and re-use if found
            for (i = 0; i < mVoiceGroupCount; i++)
            {
                if (mVoiceGroup[i].Length == 0)
                {
                    Handle[] groupa = new Handle[16];
                    if (groupa == null)
                    {
                        unlockAudioMutex_internal();
                        return default;
                    }
                    mVoiceGroup[i] = groupa;
                    //groupa[0] = 16;
                    groupa[0] = default;
                    unlockAudioMutex_internal();
                    return new Handle(0xfffff000 | i);
                }
            }
            if (mVoiceGroupCount == 4096)
            {
                unlockAudioMutex_internal();
                return default;
            }
            uint oldcount = mVoiceGroupCount;
            if (mVoiceGroupCount == 0)
            {
                mVoiceGroupCount = 4;
            }
            mVoiceGroupCount *= 2;
            Handle[][] vg = new Handle[mVoiceGroupCount][];
            if (vg == null)
            {
                mVoiceGroupCount = oldcount;
                unlockAudioMutex_internal();
                return default;
            }
            for (i = 0; i < oldcount; i++)
            {
                vg[i] = mVoiceGroup[i];
            }

            for (; i < mVoiceGroupCount; i++)
            {
                vg[i] = Array.Empty<Handle>();
            }

            //delete[] mVoiceGroup;
            mVoiceGroup = vg;
            i = oldcount;
            Handle[] groupb = new Handle[16];
            if (groupb == null)
            {
                unlockAudioMutex_internal();
                return default;
            }
            mVoiceGroup[i] = groupb;
            //groupb[0] = 16;
            groupb[0] = default;
            unlockAudioMutex_internal();
            return new Handle(0xfffff000 | i);
        }

        // Destroy a voice group.
        public SOLOUD_ERRORS destroyVoiceGroup(Handle aVoiceGroupHandle)
        {
            if (!isVoiceGroup(aVoiceGroupHandle))
                return SOLOUD_ERRORS.INVALID_PARAMETER;
            int c = (int)(aVoiceGroupHandle.Value & 0xfff);

            lockAudioMutex_internal();
            //delete[] mVoiceGroup[c];
            mVoiceGroup[c] = Array.Empty<Handle>();
            unlockAudioMutex_internal();
            return SOLOUD_ERRORS.SO_NO_ERROR;
        }

        // Add a voice handle to a voice group
        public SOLOUD_ERRORS addVoiceToGroup(Handle aVoiceGroupHandle, Handle aVoiceHandle)
        {
            if (!isVoiceGroup(aVoiceGroupHandle))
                return SOLOUD_ERRORS.INVALID_PARAMETER;

            // Don't consider adding invalid voice handles as an error, since the voice may just have ended.
            if (!isValidVoiceHandle(aVoiceHandle))
                return SOLOUD_ERRORS.SO_NO_ERROR;

            trimVoiceGroup_internal(aVoiceGroupHandle);

            int c = (int)(aVoiceGroupHandle.Value & 0xfff);

            lockAudioMutex_internal();

            Handle[] group = mVoiceGroup[c];
            for (uint i = 0; i < group.Length; i++)
            {
                if (group[i] == aVoiceHandle)
                {
                    unlockAudioMutex_internal();
                    return SOLOUD_ERRORS.SO_NO_ERROR; // already there
                }

                if (group[i].Value == 0)
                {
                    group[i] = aVoiceHandle;
                    group[i + 1] = default;

                    unlockAudioMutex_internal();
                    return SOLOUD_ERRORS.SO_NO_ERROR;
                }
            }

            // Full group, allocate more memory
            int newLength = group.Length != 0 ? group.Length * 2 : 16;
            Handle[] n = new Handle[newLength];
            if (n == null)
            {
                unlockAudioMutex_internal();
                return SOLOUD_ERRORS.OUT_OF_MEMORY;
            }

            for (uint i = 0; i < group.Length; i++)
                n[i] = group[i];
            n[group.Length] = aVoiceHandle;
            n[group.Length + 1] = default;
            //n[0] = (uint)n.Length;
            //delete[] mVoiceGroup[c];
            mVoiceGroup[c] = n;
            unlockAudioMutex_internal();
            return SOLOUD_ERRORS.SO_NO_ERROR;
        }

        // Is this handle a valid voice group?
        public bool isVoiceGroup(Handle aVoiceGroupHandle)
        {
            if ((aVoiceGroupHandle.Value & 0xfffff000) != 0xfffff000)
                return false;
            uint c = aVoiceGroupHandle.Value & 0xfff;
            if (c >= mVoiceGroupCount)
                return false;

            lockAudioMutex_internal();
            bool res = mVoiceGroup[c].Length != 0;
            unlockAudioMutex_internal();

            return res;
        }

        // Is this voice group empty?
        public bool isVoiceGroupEmpty(Handle aVoiceGroupHandle)
        {
            // If not a voice group, yeah, we're empty alright..
            if (!isVoiceGroup(aVoiceGroupHandle))
                return true;
            trimVoiceGroup_internal(aVoiceGroupHandle);
            int c = (int)(aVoiceGroupHandle.Value & 0xfff);

            lockAudioMutex_internal();
            bool res = mVoiceGroup[c].Length != 0;
            unlockAudioMutex_internal();

            return res;
        }

        // Remove all non-active voices from group
        internal void trimVoiceGroup_internal(Handle aVoiceGroupHandle)
        {
            if (!isVoiceGroup(aVoiceGroupHandle))
                return;
            int c = (int)(aVoiceGroupHandle.Value & 0xfff);

            lockAudioMutex_internal();
            Handle[] group = mVoiceGroup[c];

            // empty group
            if (group.Length == 0)
            {
                unlockAudioMutex_internal();
                return;
            }

            // first item in voice group is number of allocated indices
            for (uint i = 0; i < group.Length; i++)
            {
                // If we hit a voice in the group that's not set, we're done
                if (group[i].Value == 0)
                {
                    unlockAudioMutex_internal();
                    return;
                }

                unlockAudioMutex_internal();
                while (!isValidVoiceHandle(group[i])) // function locks mutex, so we need to unlock it before the call
                {
                    lockAudioMutex_internal();
                    // current index is an invalid handle, move all following handles backwards
                    for (uint j = i; j < group.Length - 1; j++)
                    {
                        group[j] = group[j + 1];
                        // not a full group, we can stop copying
                        if (group[j].Value == 0)
                            break;
                    }
                    // be sure to mark the last one as unused in any case
                    group[group.Length - 1] = default;
                    // did we end up with an empty group? we're done then
                    if (group[i].Value == 0)
                    {
                        unlockAudioMutex_internal();
                        return;
                    }
                    unlockAudioMutex_internal();
                }
                lockAudioMutex_internal();
            }
            unlockAudioMutex_internal();
        }

        // Get pointer to the zero-terminated array of voice handles in a voice group
        internal ArraySegment<Handle> voiceGroupHandleToArray_internal(Handle aVoiceGroupHandle)
        {
            if ((aVoiceGroupHandle.Value & 0xfffff000) != 0xfffff000)
                return default;
            uint c = aVoiceGroupHandle.Value & 0xfff;
            if (c >= mVoiceGroupCount)
                return default;
            Handle[] group = mVoiceGroup[c];
            return new ArraySegment<Handle>(group);
        }
    }
}
