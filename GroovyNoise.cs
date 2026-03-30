using Log = ZAMERT.ZAMERTLogger;

namespace ZAMERT
{
    public class GroovyNoise : ZAMERTInteractable
    {
        public new GNDTO Base { get; set; }

        protected virtual void Start()
        {
            Base = base.Base as GNDTO;
            ZAMERTPlugin.Singleton.GroovyNoises.Add(this);
        }

        public virtual void Fire()
        {
            if (Base == null || Base.Settings == null) return;
            GMDTO.Execute(Base.Settings, new ModuleGeneralArguments
            {
                Schematic = OSchematic,
                Transform = transform,
            });
        }

        protected virtual void Update()
        {
            if (Active)
                Fire();
            Active = false;
        }
    }

    public class FGroovyNoise : GroovyNoise
    {
        public new FGNDTO Base { get; set; }

        protected override void Start()
        {
            Base = ((ZAMERTInteractable)this).Base as FGNDTO;
            ZAMERTPlugin.Singleton.GroovyNoises.Add(this);
        }

        public void FireF(FunctionArgument args)
        {
            if (Base == null || Base.Settings == null) return;
            FGMDTO.Execute(Base.Settings, args);
        }

        protected override void Update()
        {
            if (Active)
                FireF(new FunctionArgument(this));
            Active = false;
        }
    }
}
