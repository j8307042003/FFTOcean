using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ParticleEmitter<T>
{

    public delegate CustomEmitInfo EmitterFunc(ParticleEmitterParam param);
    public delegate T CustomUpdateFunc(Particle particle, T customData);

    public struct CustomEmitInfo
    {
        public Vector3 position;
        public Vector3 velocity;
        public float lifeSpan;
    }


    public struct ParticleEmitterParam
    {
        public int maxParticle;
        public float ratio;
        public float lifeSpan;
        public Action<Vector3> CustomEmitVelocity;
        public EmitterFunc CustomEmitter;
        public CustomUpdateFunc CustomUpdater;
        public Type customParticleDataType;
    };

    public struct Particle
    {
        public float life;
        public Vector3 position;
        public Vector3 velocity;
    }

    public struct ParticlesData
    {
        public int count;
        public Particle[] particles;
        public T[] particlesCustomData;
    }

    int maxParticle = 10;
    float ratio;
    float lifeSpan;
    Action<Vector3> CustomEmitVelocity;
    EmitterFunc EmitFunc;
    CustomUpdateFunc CustomUpdater;
    ParticleEmitterParam param;
    float time;

    Particle[] particleData;
    T[] particleCustomData;
    int particleCount;

    public ParticleEmitter(ParticleEmitterParam param)
    {
        time = 0;

        maxParticle = param.maxParticle;
        ratio = param.ratio;
        lifeSpan = param.lifeSpan;
        CustomEmitVelocity = param.CustomEmitVelocity;
        EmitFunc = param.CustomEmitter != null ? param.CustomEmitter : Emitter;
        CustomUpdater = param.CustomUpdater != null ? param.CustomUpdater : null;
        this.param = param;

        particleData = new Particle[param.maxParticle];
        particleCustomData = new T[param.maxParticle];
        particleCount = 0;
    }

    CustomEmitInfo Emitter(ParticleEmitterParam param)
    {
        CustomEmitInfo info = new CustomEmitInfo();
        return info;
    }

    public ParticlesData GetParticles()
    {
        ParticlesData data = new ParticlesData();
        data.count = particleCount;
        data.particles = particleData;
        data.particlesCustomData = particleCustomData;
        return data;
    }

    public void Emit()
    {
        if (particleCount >= particleData.Length) return;
        CustomEmitInfo info = EmitFunc(param);
        particleData[particleCount].life = 0;
        particleData[particleCount].position = info.position;
        particleData[particleCount].velocity = info.velocity;
        particleCount++;
    }

    public void Emit(CustomEmitInfo info, T customData)
    {
        if (particleCount >= particleData.Length) return;
        particleData[particleCount].life = info.lifeSpan;
        particleData[particleCount].position = info.position;
        particleData[particleCount].velocity = info.velocity;
        particleCustomData[particleCount] = customData;
        particleCount++;
    }
    

    public void Update(float deltaTime)
    {
        time += deltaTime;

        int emitCount = (int)(time / ratio);
        time -= emitCount * ratio;
        time = Mathf.Max(0, time);

        for (int i = particleCount-1; i >= 0; i--)
        {
            particleData[i].life += deltaTime;
            particleData[i].position += particleData[i].velocity * deltaTime;
            if (CustomUpdater != null) particleCustomData[i] = CustomUpdater(particleData[i], particleCustomData[i]);

            if (particleData[i].life >= param.lifeSpan)
            {
                var tmp = particleData[i];
                var tmpCustom = particleCustomData[i];
                particleData[i] = particleData[particleCount - 1];
                particleData[particleCount - 1] = tmp;

                particleCustomData[i] = particleCustomData[particleCount - 1];
                particleCustomData[particleCount - 1] = tmpCustom;

                particleCount--;
            }
        }


        for (int i = 0; i < emitCount; i++)
        {
            Emit();
        }




    }

}
