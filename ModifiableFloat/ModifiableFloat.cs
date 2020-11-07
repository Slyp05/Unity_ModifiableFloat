using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;

/*
    
DESCRIPTION:
    ModifiableFloat created by Lucas Sarkadi

    Creative Commons Zero v1.0 Universal licence, 
    meaning it's free to use in any project with no need to ask permission or credits the author.

    Check out the github page for more informations:
    https://github.com/Slyp05/Unity_ModifiableFloat/

USAGE:
    ModifiableFloat is a custom class intended to mimic a float with the added benefit of being  
    able to modify it concurrently from multiples other classes.

    it will implictly cast to a float (using the computed value) and a float can implictly be cast
    to a ModifiableFloat (setting it's base value).
    If needed you can use ".Base" and ".Value".

    Computed value is calculated from modifier which are linked to an object instance (+ a string, 
    usefull if you have multiple modifier on a single instance).
    You can choose the order of application of modifier, for two modifier with the same order the
    calculation will be: 
    Set then Add/Substract then Multiply/Divide then Modulo then Min/Max and then Custom functions.

    Reapplying the same exact modifier won't recompute the value. You can therefore spam the same 
    modifier every frame.

    You can cast the ModfiableFloat into an integer using ".Round", ".Floor" and ".Ceil"

    You can debug the ModifiableFloat using:
        ".DebugString()"        which will return a string describing how the value was computed
        ".IgnoreModification"   which is a boolean allowing you to ignore all modifiers
    Both accessible in the inspector.

EXAMPLE:
    {
        ModifiableFloat mf = 10f;

        mf.Modify_Add(this, "uniqueName", 10.5f, 5);
  (or)  mf[this, "uniqueName"].Add(10.5f, 5);
  (or)  mf[this, "uniqueName"] += 10.5f; // no order
        
        Debug.Log(mf); // ouptut "20.5f";
    }

MODIFIERS LIST:
    Function_Name()     [].Func()   [] ?= X     : description

    Modify_Set()        [].Set()    [] = X      : Set to X
    Modify_Add()        [].Add()    [] += X     : Add X
    Modify_Substract()  [].Sub()    [] -= X     : Substract X
    Modify_Multiply()   [].Mul()    [] *= X     : Multiply by X
    Modify_Divide()     [].Div()    [] /= X     : Divide by X
    Modify_Modulo()     [].Mod()    [] %= X     : Modulo by X
    Modify_Minimum()    [].Min()    [] >>= X    : Set the value to be at least X
    Modify_Maximum()    [].Max()    [] <<= X    : Set the value to be no more than X
    Modify_Custom()     [].Cus()    [] |= Func  : Apply a custom function to the value
    Modify_Clear()      [].Clr()    --[]        : Clear the modifier

WARNING:
    You should probably clear all modifier from a MonoBehaviour when disabling it using:

    void OnDisable()
    {
        modifiableFloat.ClearAllMyModifications(this);
    }

*/
[System.Serializable] public class ModifiableFloat
{
    // Property Drawer communicator
#if UNITY_EDITOR
    public void _EDITOR_ONLY_ForceProcess() { gottaProcess = true; }
    public int _EDITOR_ONLY_GetNumberOfModifier()
    {
        int nbOfModifier = 0;
        for (int type = 0; type < modifierCount; type++)
            nbOfModifier += allModifiers[type].Count;
        return nbOfModifier;
    }
#endif

    // Public / Editor variables
    [SerializeField]
    float _baseValue;
    public float Base
    {
        get { return _baseValue; }
        set { if (_baseValue == value)
                return ;
            if (_ignoreModification || allModifiers[(int)ModifierType.Set].Count == 0)
                gottaProcess = true;
            _baseValue = value;
        }
    }

    [SerializeField]
    bool _ignoreModification;
    public bool IgnoreModification
    {
        get { return _ignoreModification; }
        set { if (_ignoreModification == value)
                return;
            gottaProcess = true;
            _ignoreModification = value;
        }
    }

    float _computedValue;
    public float Value
    {
        get {   ProcessValue();
                return _computedValue; }
    }

    // Modifier
    const int modifierCount = 7;
    enum ModifierType { Set, Add, Mul, Mod, Min, Max, Custom };

    public delegate float CustomAction(float param);

    struct AModifier
    {
        public int parentID;
        public string name;
        
        public float val;
        public int order;

        public CustomAction action;

        public AModifier(int _parentID, string _name, float _val, int _order)
        {
            parentID = _parentID;
            name = _name;
            val = _val;
            order = _order;
            action = null;
        }

        public AModifier(int _parentID, string _name, CustomAction _action, int _order)
        {
            parentID = _parentID;
            name = _name;
            val = 0;
            order = _order;
            action = _action;
        }
    }

    // Public custom Methods
    public void Modify_Set(Object obj, float val, int order = 0)
    { Modify_Set(obj, string.Empty, val, order); }

    public void Modify_Set(Object obj, string name, float val, int order = 0)
    {
        if (AlreadyCreated(ModifierType.Set, obj.GetInstanceID(), name, val, null, order))
            return;
        Create(ModifierType.Set, obj.GetInstanceID(), name, val, null, order);
        gottaProcess = true;
    }


    public void Modify_Substract(Object obj, float val, int order = 0)
    { Modify_Substract(obj, string.Empty, val, order); }

    public void Modify_Substract(Object obj, string name, float val, int order = 0)
    { Modify_Add(obj, name, -val, order); }

    public void Modify_Add(Object obj, float val, int order = 0)
    { Modify_Add(obj, string.Empty, val, order); }

    public void Modify_Add(Object obj, string name, float val, int order = 0)
    {
        if (AlreadyCreated(ModifierType.Add, obj.GetInstanceID(), name, val, null, order))
            return;
        Create(ModifierType.Add, obj.GetInstanceID(), name, val, null, order);
        gottaProcess = true;
    }


    public void Modify_Divide(Object obj, float val, int order = 0)
    { Modify_Divide(obj, string.Empty, val, order); }

    public void Modify_Divide(Object obj, string name, float val, int order = 0)
    { Modify_Multiply(obj, name, 1 / val, order); }

    public void Modify_Multiply(Object obj, float val, int order = 0)
    { Modify_Multiply(obj, string.Empty, val, order); }

    public void Modify_Multiply(Object obj, string name, float val, int order = 0)
    {
        if (AlreadyCreated(ModifierType.Mul, obj.GetInstanceID(), name, val, null, order))
            return;
        Create(ModifierType.Mul, obj.GetInstanceID(), name, val, null, order);
        gottaProcess = true;
    }


    public void Modify_Modulo(Object obj, float val, int order = 0)
    { Modify_Modulo(obj, string.Empty, val, order); }

    public void Modify_Modulo(Object obj, string name, float val, int order = 0)
    {
        if (AlreadyCreated(ModifierType.Mod, obj.GetInstanceID(), name, val, null, order))
            return;
        Create(ModifierType.Mod, obj.GetInstanceID(), name, val, null, order);
        gottaProcess = true;
    }


    public void Modify_Minimum(Object obj, float val, int order = 0)
    { Modify_Minimum(obj, string.Empty, val, order); }

    public void Modify_Minimum(Object obj, string name, float val, int order = 0)
    {
        if (AlreadyCreated(ModifierType.Min, obj.GetInstanceID(), name, val, null, order))
            return;
        Create(ModifierType.Min, obj.GetInstanceID(), name, val, null, order);
        gottaProcess = true;
    }

    public void Modify_Maximum(Object obj, float val, int order = 0)
    { Modify_Maximum(obj, string.Empty, val, order); }

    public void Modify_Maximum(Object obj, string name, float val, int order = 0)
    {
        if (AlreadyCreated(ModifierType.Max, obj.GetInstanceID(), name, val, null, order))
            return;
        Create(ModifierType.Max, obj.GetInstanceID(), name, val, null, order);
        gottaProcess = true;
    }


    public void Modify_Custom(Object obj, CustomAction action, int order = 0)
    { Modify_Custom(obj, string.Empty, action, order); }

    public void Modify_Custom(Object obj, string name, CustomAction action, int order = 0)
    {
        if (AlreadyCreated(ModifierType.Custom, obj.GetInstanceID(), name, 0, action, order))
            return ;
        Create(ModifierType.Custom, obj.GetInstanceID(), name, 0, action, order);
        gottaProcess = true;
    }


    public void Modify_Clear(Object obj)
    { Modify_Clear(obj, string.Empty); }

    public void Modify_Clear(Object obj, string name)
    {
        if (!Exist(obj.GetInstanceID(), name))
            return ;

        ClearOne(obj.GetInstanceID(), name);
        gottaProcess = true;
    }

    public void ClearAllMyModifications(Object obj)
    {
        if (!Exist(obj.GetInstanceID()))
            return;

        ClearAll(obj.GetInstanceID());
        gottaProcess = true;
    }
    
    // Private variables
    List<AModifier>[] allModifiers;

    bool roundReady;
    bool ceilReady;
    bool floorReady;

    StringBuilder debugString;

    bool gottaProcess;

    // Private custom Methods
    bool AlreadyCreated(ModifierType type, int id, string name, float val, CustomAction action, int order)
    {
        return (allModifiers[(int)type].Exists(x => (x.parentID == id && x.name == name && x.val == val &&
        x.action == action && x.order == order)));
    }

    void Create(ModifierType type, int id, string name, float val, CustomAction action, int order)
    {
        ClearOne(id, name);
        if (type == ModifierType.Custom)
            allModifiers[(int)type].Add(new AModifier(id, name, action, order));
        else
            allModifiers[(int)type].Add(new AModifier(id, name, val, order));
        allModifiers[(int)type].Sort((x, y) => x.order - y.order);
    }

    bool Exist(int id, string name)
    {
        for (int i = 0; i < modifierCount; i++)
        {
            if (allModifiers[(int)i].Exists(x => (x.parentID == id && x.name == name)))
                return true;
        }

        return false;
    }

    bool Exist(int id)
    {
        for (int i = 0; i < modifierCount; i++)
        {
            if (allModifiers[(int)i].Exists(x => (x.parentID == id)))
                return true;
        }

        return false;
    }

    void ClearOne(int id, string name)
    {
        for (int i = 0; i < modifierCount; i++)
        {
            allModifiers[i] = allModifiers[i].Where
            (x => (x.parentID != id || x.name != name)).ToList();
        }
    }

    void ClearAll(int id)
    {
        for (int i = 0; i < modifierCount; i++)
        {
            allModifiers[i] = allModifiers[i].Where
            (x => x.parentID != id).ToList();
        }
    }

    void ProcessValue()
    {
        if (!gottaProcess)
            return ;

        Math();

        gottaProcess = false;
    }

    void Math()
    {
        roundReady = false;
        ceilReady = false;
        floorReady = false;
        debugString = null;

        _computedValue = _baseValue;

        if (_ignoreModification)
            return;

        int[] it = new int[modifierCount];

        bool atLeastOne = false;
        int maxOrder = int.MinValue;
        int minOrder = int.MaxValue;
        for (int type = 0; type < modifierCount; type++)
        {
            if (allModifiers[type].Count != 0)
            {
                atLeastOne = true;
                minOrder = Mathf.Min(minOrder, allModifiers[type][0].order);
                maxOrder = Mathf.Max(maxOrder, allModifiers[type][allModifiers[type].Count - 1].order);
            }
        }

        int ord = minOrder;
        while (atLeastOne && ord <= maxOrder)
        {
            for (int type = 0; type < modifierCount; type++)
            {
                while (it[type] < allModifiers[type].Count && allModifiers[type][it[type]].order <= ord)
                {
                    float val = allModifiers[type][it[type]].val;

                    if (type == (int)ModifierType.Set)
                        _computedValue = val;
                    else if (type == (int)ModifierType.Add)
                        _computedValue += val;
                    else if (type == (int)ModifierType.Mul)
                        _computedValue *= val;
                    else if (type == (int)ModifierType.Mod)
                        _computedValue %= val;
                    else if (type == (int)ModifierType.Min)
                        _computedValue = Mathf.Max(_computedValue, val);
                    else if (type == (int)ModifierType.Max)
                        _computedValue = Mathf.Min(_computedValue, val);
                    else if (type == (int)ModifierType.Custom)
                        _computedValue = allModifiers[type][it[type]].action(_computedValue);

                    it[type] += 1;
                }
            }

            int? nextOrd = null;
            for (int type = 0; type < modifierCount; type++)
                if (it[type] < allModifiers[type].Count)
                    nextOrd = !nextOrd.HasValue ? allModifiers[type][it[type]].order
                        : Mathf.Min(nextOrd.Value, allModifiers[type][it[type]].order);
            if (!nextOrd.HasValue)
                break;
            else
                ord = nextOrd.Value;
        }
    }

    // Constructeurs
    public ModifiableFloat(ModifiableFloat copy)
    {
        _baseValue = copy._baseValue;
        _ignoreModification = copy._ignoreModification;
        _computedValue = copy._computedValue;
        gottaProcess = true;

        allModifiers = new List<AModifier>[modifierCount];
        for (int i = 0; i < modifierCount; i++)
            allModifiers[i] = new List<AModifier>(copy.allModifiers[i]);
    }

    public ModifiableFloat(float val)
    {
        this._baseValue = val;
        this._computedValue = val;
        this.gottaProcess = true;

        allModifiers = new List<AModifier>[modifierCount];
        for (int i = 0; i < modifierCount; i++)
            allModifiers[i] = new List<AModifier>();
    }

    public ModifiableFloat()
    {
        this.gottaProcess = true;

        allModifiers = new List<AModifier>[modifierCount];
        for (int i = 0; i < modifierCount; i++)
            allModifiers[i] = new List<AModifier>();
    }

    // Implicit Casts
    public static implicit operator ModifiableFloat(float toConvert)
    {
        return new ModifiableFloat(toConvert);
    }

    public static implicit operator float(ModifiableFloat toConvert)
    {
        return toConvert.Value;
    }

    // To String
    public override string ToString()
    {
        return Value.ToString();
    }

    public string DebugString()
    {
        string[] stringFormatNoName = new string[modifierCount]
        {
            "[{0}]\t{1} -> {2} = {3}\t ({4})",
            "[{0}]\t{1} + {2} = {3}\t ({4})",
            "[{0}]\t{1} * {2} = {3}\t ({4})",
            "[{0}]\t{1} % {2} = {3}\t ({4})",
            "[{0}]\t{1} > {2} = {3}\t ({4})",
            "[{0}]\t{1} < {2} = {3}\t ({4})",
            "[{0}]\tCustom({1}) = {3}\t ({4})",
        };
        string[] stringFormatWithName = new string[modifierCount]
        {
            "[{0}]\t{1} -> {2} = {3}\t ({4} | \"{5}\")",
            "[{0}]\t{1} + {2} = {3}\t ({4} | \"{5}\")",
            "[{0}]\t{1} * {2} = {3}\t ({4} | \"{5}\")",
            "[{0}]\t{1} % {2} = {3}\t ({4} | \"{5}\")",
            "[{0}]\t{1} > {2} = {3}\t ({4} | \"{5}\")",
            "[{0}]\t{1} < {2} = {3}\t ({4} | \"{5}\")",
            "[{0}]\tCustom({1}) = {3}\t ({4} | \"{5}\")",
        };

        if (debugString == null)
        {
            float result = _baseValue;
            bool hasModifier = false;

            debugString = new StringBuilder();

            int[] it = new int[modifierCount];

            bool atLeastOne = false;
            int maxOrder = int.MinValue;
            int minOrder = int.MaxValue;
            for (int type = 0; type < modifierCount; type++)
            {
                if (allModifiers[type].Count != 0)
                {
                    atLeastOne = true;
                    minOrder = Mathf.Min(minOrder, allModifiers[type][0].order);
                    maxOrder = Mathf.Max(maxOrder, allModifiers[type][allModifiers[type].Count - 1].order);
                }
            }

            int ord = minOrder;
            while (atLeastOne && ord <= maxOrder)
            {
                for (int type = 0; type < modifierCount; type++)
                {
                    while (it[type] < allModifiers[type].Count && allModifiers[type][it[type]].order <= ord)
                    {
                        if (hasModifier)
                            debugString.Append('\n');

                        AModifier modif = allModifiers[type][it[type]];
                        float val = allModifiers[type][it[type]].val;

                        hasModifier = true;
                        float newRes = result;

                        if (type == (int)ModifierType.Set)
                            newRes = val;
                        else if (type == (int)ModifierType.Add)
                            newRes += val;
                        else if (type == (int)ModifierType.Mul)
                            newRes *= val;
                        else if (type == (int)ModifierType.Mod)
                            newRes %= val;
                        else if (type == (int)ModifierType.Min)
                            newRes = Mathf.Max(newRes, val);
                        else if (type == (int)ModifierType.Max)
                            newRes = Mathf.Min(newRes, val);
                        else if (type == (int)ModifierType.Custom)
                            newRes = modif.action(result);

                        string objectName = modif.parentID.ToString();
#if UNITY_EDITOR
                        Object obj = UnityEditor.EditorUtility.InstanceIDToObject(modif.parentID);
                        if (obj != null)
                            objectName = obj.name;
#endif
                        if (string.IsNullOrEmpty(modif.name))
                            debugString.AppendFormat(stringFormatNoName[type], modif.order.ToString(),
                            result.ToString(), modif.val.ToString(), newRes.ToString(), objectName);
                        else
                            debugString.AppendFormat(stringFormatWithName[type], modif.order.ToString(),
                            result.ToString(), modif.val.ToString(), newRes.ToString(), objectName, modif.name);

                        result = newRes;

                        it[type] += 1;
                    }
                }

                int? nextOrd = null;
                for (int type = 0; type < modifierCount; type++)
                    if (it[type] < allModifiers[type].Count)
                        nextOrd = !nextOrd.HasValue ? allModifiers[type][it[type]].order
                            : Mathf.Min(nextOrd.Value, allModifiers[type][it[type]].order);
                if (!nextOrd.HasValue)
                    break;
                else
                    ord = nextOrd.Value;
            }

            if (!hasModifier)
                debugString.Append("No Modifier");
        }

        return debugString.ToString();
    }

    // Operator Overload
    public static ModifiableFloat operator +(ModifiableFloat mFloat, float f)
    {   ModifiableFloat newFloat = new ModifiableFloat(mFloat);
        newFloat.Base += f;
        return newFloat; }

    public static ModifiableFloat operator -(ModifiableFloat mFloat, float f)
    {   ModifiableFloat newFloat = new ModifiableFloat(mFloat);
        newFloat.Base -= f;
        return newFloat; }

    public static ModifiableFloat operator *(ModifiableFloat mFloat, float f)
    {   ModifiableFloat newFloat = new ModifiableFloat(mFloat);
        newFloat.Base *= f;
        return newFloat; }

    public static ModifiableFloat operator /(ModifiableFloat mFloat, float f)
    {   ModifiableFloat newFloat = new ModifiableFloat(mFloat);
        newFloat.Base /= f;
        return newFloat; }

    public static ModifiableFloat operator %(ModifiableFloat mFloat, float f)
    {   ModifiableFloat newFloat = new ModifiableFloat(mFloat);
        newFloat.Base %= f;
        return newFloat; }


    public static ModifiableFloat operator +(ModifiableFloat mFloat)
    {   ModifiableFloat newFloat = new ModifiableFloat(mFloat);
        return newFloat; }

    public static ModifiableFloat operator -(ModifiableFloat mFloat)
    {   ModifiableFloat newFloat = new ModifiableFloat(mFloat);
        newFloat.Base *= -1;
        return newFloat; }

    public static ModifiableFloat operator ++(ModifiableFloat mFloat)
    {   ModifiableFloat newFloat = new ModifiableFloat(mFloat);
        newFloat.Base += 1;
        return newFloat; }

    public static ModifiableFloat operator --(ModifiableFloat mFloat)
    {   ModifiableFloat newFloat = new ModifiableFloat(mFloat);
        newFloat.Base -= 1;
        return newFloat; }


    // Simple Access
    public class ModifierAccess
    {
        ModifiableFloat _parent;
        Object _obj;
        string _name;

        public float? _valToSet;

        public ModifierAccess(float valToSet)
        { _valToSet = valToSet; }

        public ModifierAccess(ModifiableFloat parent, Object obj, string name)
        { _parent = parent; _obj = obj; _name = name; }

        public void Set(float val, int order = 0) { _parent.Modify_Set(_obj, _name, val, order); }
        public void Add(float val, int order = 0) { _parent.Modify_Add(_obj, _name, val, order); }
        public void Sub(float val, int order = 0) { _parent.Modify_Substract(_obj, _name, val, order); }
        public void Mul(float val, int order = 0) { _parent.Modify_Multiply(_obj, _name, val, order); }
        public void Div(float val, int order = 0) { _parent.Modify_Divide(_obj, _name, val, order); }
        public void Mod(float val, int order = 0) { _parent.Modify_Modulo(_obj, _name, val, order); }
        public void Min(float val, int order = 0) { _parent.Modify_Minimum(_obj, _name, val, order); }
        public void Max(float val, int order = 0) { _parent.Modify_Maximum(_obj, _name, val, order); }
        public void Cus(CustomAction action, int order = 0) { _parent.Modify_Custom(_obj, _name, action, order); }
        public void Clr() { _parent.Modify_Clear(_obj, _name); }

        public static ModifierAccess operator +(ModifierAccess access, float f)
        { access.Add(f); return access; }

        public static ModifierAccess operator -(ModifierAccess access, float f)
        { access.Sub(f); return access; }

        public static ModifierAccess operator *(ModifierAccess access, float f)
        { access.Mul(f); return access; }

        public static ModifierAccess operator /(ModifierAccess access, float f)
        { access.Div(f); return access; }

        public static ModifierAccess operator %(ModifierAccess access, float f)
        { access.Mod(f); return access; }

        public static ModifierAccess operator >>(ModifierAccess access, int i)
        { access.Min(i); return access; }

        public static ModifierAccess operator <<(ModifierAccess access, int i)
        { access.Max(i); return access; }

        public static ModifierAccess operator |(ModifierAccess access, CustomAction action)
        { access.Cus(action); return access; }

        public static ModifierAccess operator --(ModifierAccess access)
        { access.Clr(); return access; }

        public static implicit operator ModifierAccess(float valToSet)
        { return new ModifierAccess(valToSet); }
    }

    public ModifierAccess this[Object obj]
    {
        get { return this[obj, string.Empty]; }
        set { this[obj, string.Empty] = value; }
    }

    public ModifierAccess this[Object obj, string name]
    {
        get { return new ModifierAccess(this, obj, name); }
        set { if (value._valToSet.HasValue)
                Modify_Set(obj, name, value._valToSet.Value);
        }
    }

    // To int
    int _round;
    public int Round { get {

            if (!roundReady)
            {   _round = Mathf.RoundToInt(_computedValue);
                roundReady = true; }
            return _round;
    } }

    int _floor;
    public int Floor { get {

            if (!floorReady)
            {   _floor = Mathf.FloorToInt(_computedValue);
                floorReady = true; }
            return _floor;
    } }

    int _ceil;
    public int Ceil { get {

            if (!ceilReady)
            {   _ceil = Mathf.CeilToInt(_computedValue);
                ceilReady = true; }
            return _ceil;
    } }
}