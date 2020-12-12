﻿using Microsoft.Xna.Framework;
using SharpVR;

namespace Alex.API.Graphics
{
    public class VrCameraWrapper : ICameraWrapper
    {
        private readonly VrContext _vrContext;
        private Eye _eye;
        private Matrix _hmd;
        internal VrCameraWrapper(VrContext vrContext)
        {
            _vrContext = vrContext;
        }

        internal void Update(Eye eye, Matrix hmd)
        {
            _eye = eye;
            _hmd = hmd;
        }
        
        public void PreDraw(ICamera camera)
        {
            Matrix projection;
            var eyeMatrix = Matrix.Identity;
            _vrContext.GetProjectionMatrix(_eye, camera.NearDistance, camera.FarDistance, out var hmdProjection);
            _vrContext.GetEyeMatrix(_eye, out var hmdEye);
            projection = hmdProjection.ToMg();
            eyeMatrix = hmdEye.ToMg();

            var view = Matrix.CreateLookAt(camera.Position, camera.Target, Vector3.Up);
            var forward = Vector3.TransformNormal(camera.Forward, Matrix.Invert(_hmd * eyeMatrix));
            camera.ProjectionMatrix = projection;
            camera.ViewMatrix = view *Matrix.Invert( _hmd * eyeMatrix);
        }
    }
}