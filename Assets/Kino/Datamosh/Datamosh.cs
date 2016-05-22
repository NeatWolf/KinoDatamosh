﻿//
// Kino/Datamosh - Video compression artifact effect
//
// Copyright (C) 2016 Keijiro Takahashi
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
using UnityEngine;

namespace Kino
{
    [RequireComponent(typeof(Camera))]
    public class Datamosh : MonoBehaviour
    {
        #region Public properties and methods

        /// Start glitching.
        public void Glitch()
        {
            _sequence = 1;
        }

        /// Force to end glitching.
        public void Reset()
        {
            _sequence = 0;
        }

        #endregion

        #region Private properties

        [SerializeField] Shader _shader;

        Material _material;

        RenderTexture _lastFrame;
        int _sequence;

        #endregion

        #region MonoBehaviour functions

        void OnEnable()
        {
            var shader = Shader.Find("Hidden/Kino/Datamosh");
            _material = new Material(shader);
            _material.hideFlags = HideFlags.DontSave;

            GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth | DepthTextureMode.MotionVectors;

            _sequence = 0;
        }

        void OnDisable()
        {
            if (_lastFrame != null)
            {
                RenderTexture.ReleaseTemporary(_lastFrame);
                _lastFrame = null;
            }

            DestroyImmediate(_material);
            _material = null;
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (_sequence == 0)
            {
                // Store and hold this frame.
                if (_lastFrame != null)
                    RenderTexture.ReleaseTemporary(_lastFrame);

                _lastFrame = RenderTexture.GetTemporary(source.width, source.height);

                Graphics.Blit(source, _lastFrame);

                // Blit without effect.
                Graphics.Blit(source, destination);
            }
            else if (_sequence == 1)
            {
                // Discard this frame; simply blit the last frame without effect.
                Graphics.Blit(_lastFrame, destination);
                _sequence++;
            }
            else
            {
                // Downsample the motion vector buffer.
                var mv = RenderTexture.GetTemporary(source.width / 32, source.height / 32, 0, RenderTextureFormat.RGHalf);
                mv.filterMode = FilterMode.Point;
                Graphics.Blit(null, mv, _material, 0);

                // Moshing
                var nextFrame = RenderTexture.GetTemporary(source.width, source.height);
                _material.SetTexture("_MotionTex", mv);
                Graphics.Blit(_lastFrame, nextFrame, _material, 1);

                // Release the last frame.
                RenderTexture.ReleaseTemporary(_lastFrame);
                _lastFrame = nextFrame;

                // Release the downsampled motion vector buffer.
                RenderTexture.ReleaseTemporary(mv);

                // Blit the result.
                Graphics.Blit(nextFrame, destination);
            }
        }

        #endregion
    }
}
