using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.Reflection;
using System.Windows.Forms;
using System.IO;

//pour automatiser sauvegarde Variables vers Ini ou Base de Registre

////////////// ajouter les retours de test write_interne !!!!!!!

//version 10.03.09, verifier modifs sur classe Ini à la maison, MAJ dans classe inireg

namespace IniReg
{
    abstract public class c_IniRegBase //classe Mere pour traiter variable, sauvegarde dans classe enfant
    {
        protected string v_AdresseBase;
        protected string v_ParentKeyName;
        protected bool v_IsIni;

        protected abstract bool m_writeInterne(object Variable, string NomVariable);
        protected abstract string m_readInterne(string NomVariable);
        protected abstract void m_WriteIni();

        public bool m_Write(object Variable, string NomVariable)
        {
            bool v_result = true;
            //cherche si variable seule ou dans classe
            if (Variable.GetType().IsClass)
            {
                if (NomVariable != "") { v_ParentKeyName = "\\" + NomVariable; } else { v_ParentKeyName = ""; }
                var v_fields = Variable.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);
                v_fields = v_fields.Concat(Variable.GetType().GetFields()).ToArray();
                foreach (FieldInfo v_field in v_fields)
                {
                    var v_test = v_field.GetValue(Variable);
                    if (v_test != null) //si null, rien à sauver !!
                    {
                        if ((v_test.GetType().IsClass) && (v_test as string == null)) //ce champ est une classe
                        {
                            v_result &= m_Write(v_field.GetValue(Variable), NomVariable + "\\" + v_field.Name);
                            v_ParentKeyName = m_remonterParentKeyName(v_ParentKeyName);
                        }
                        else
                        { v_result &= m_writeInterne(v_field.GetValue(Variable), v_field.Name); }
                    }
                }
            }
            else
            {
                v_ParentKeyName = "";
                v_result &= m_writeInterne(Variable, NomVariable);
            }
            if (v_IsIni) //sauvegarde le fichier Ini à partir du "fichier" dans c_INI
            {
                m_WriteIni();
            }
            return v_result;
        }

        string m_remonterParentKeyName(string vl_key)
        {
            int v_index = 0;
            for (int i = vl_key.Length-1; i >= 0; i--)
            {
                if (vl_key[i] == '\\') { v_index = i; break; }
            }
            return vl_key.Substring(0, v_index);
        }

        public T m_Read<T>(T Variable, string NomVariable)
        {
            if (Variable.GetType().IsClass)
            {
                if (NomVariable != "") { v_ParentKeyName = "\\" + NomVariable; } else { v_ParentKeyName = ""; }
                var v_fields = Variable.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);
                v_fields = v_fields.Concat(Variable.GetType().GetFields()).ToArray();
                foreach (FieldInfo v_field in v_fields)
                {
                    //cree v_F car on ne peut acceder a Variable par Foreach
                    FieldInfo v_f = Variable.GetType().GetField(v_field.Name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);
                    if (v_f == null) { v_f = Variable.GetType().GetField(v_field.Name); }
                    object v_result = null;
                    if ((v_f.GetValue(Variable) != null) && (v_f.GetValue(Variable).GetType().IsClass) && (v_f.GetValue(Variable) as string == null))
                    {
                        v_result = m_Read(v_f.GetValue(Variable), NomVariable + "\\" + v_field.Name);
                        v_ParentKeyName = m_remonterParentKeyName(v_ParentKeyName);
                    }
                    else
                    {
                        string v_champ = m_readInterne(v_field.Name);
                        var v_type = v_f.GetValue(Variable);
                        if ((v_type != null) && (v_f.GetValue(Variable).GetType().IsEnum))
                        {
                            try { v_result = Enum.Parse(v_f.GetValue(Variable).GetType(), v_champ); }
                            catch { }
                        }
                        else
                        { v_result = m_getValue(v_f.FieldType.Name.ToLower(), v_champ); }
                    }
                    if (v_result != null)
                    { v_f.SetValue(Variable, v_result); }
                }
                return (Variable);
            }
            else
            {
                v_ParentKeyName = "";
                try { return (T)m_getValue(Variable.GetType().Name.ToLower(), m_readInterne(NomVariable)); }//(Variable, NomVariable));
                catch { return default(T); }
            }
        }

        object m_getValue(string typeField, string v_ValueSousFormeString)
        {
            object v_result = null;
            try
            {
                switch (typeField)
                {
                    case "boolean":
                        if (v_ValueSousFormeString.ToUpper() == "TRUE") { v_result = true; }
                        if (v_ValueSousFormeString.ToUpper() == "FALSE") { v_result = false; }
                        //sinon affecte tjs false si ne trouve rien, sans tenir compte v_ValueSousFormeStringeur par defaut
                        break;
                    case "byte":
                    case "sbyte": v_result = byte.Parse(v_ValueSousFormeString); break;
                    case "char": v_result = char.Parse(v_ValueSousFormeString); break;
                    case "datetime": v_result = DateTime.Parse(v_ValueSousFormeString); break;
                    case "timespan": v_result = TimeSpan.Parse(v_ValueSousFormeString); break;
                    case "decimal": v_result = decimal.Parse(v_ValueSousFormeString); break;
                    case "double": v_result = double.Parse(v_ValueSousFormeString); break;
                    case "int16":
                    case "int32":
                    case "int64":
                    case "uin16":
                    case "uint32":
                    case "uint64": v_result = int.Parse(v_ValueSousFormeString); break;
                    case "single": v_result = Single.Parse(v_ValueSousFormeString); break;
                    case "string": v_result = v_ValueSousFormeString; break;
                }
                return v_result;
            }
            catch { return null; }
        }
    }

    public class c_Reg : c_IniRegBase
    {
        RegistryKey v_Key;// = Registry.CurrentUser;

        public c_Reg(string AdresseBase)
        {
            v_AdresseBase = "Software\\" + AdresseBase;
            v_Key = Registry.CurrentUser.CreateSubKey(v_AdresseBase);
            v_IsIni = false;
        }

        ~c_Reg()
        {
            try { v_Key.Close(); }
            catch { }
        }

        protected override bool m_writeInterne(object Variable, string NomVariable)
        {
            try
            {
                v_Key = Registry.CurrentUser.CreateSubKey(v_AdresseBase + v_ParentKeyName);
                v_Key.OpenSubKey(v_AdresseBase + v_ParentKeyName);
                v_Key.SetValue(NomVariable, Variable);
                v_Key.Flush();
                return true;
            }
            catch { return false; }
        }

        protected override string m_readInterne(string NomVariable)
        {
            try
            {
                v_Key = Registry.CurrentUser.OpenSubKey(v_AdresseBase + v_ParentKeyName);
                return v_Key.GetValue(NomVariable).ToString();
            }
            catch { return ""; }
        }

        protected override void m_WriteIni() { }
    }

    public class c_Ini : c_IniRegBase
    {
        List<string> v_fichierIni;

        public c_Ini(string AdresseBase)
        {
            v_IsIni = true;
            v_fichierIni = new List<string>();
            v_AdresseBase = new FileInfo(Application.ExecutablePath).DirectoryName + "\\" + AdresseBase;
            try
            {
                StreamReader sR = new StreamReader(v_AdresseBase);
                v_fichierIni.AddRange(sR.ReadToEnd().Split(("\r\n").ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
                sR.Close();
            }
            catch { }//pas de fichier Ini trouve
        }

        protected override bool m_writeInterne(object Variable, string NomVariable)
        {
            try
            {
                //cherche le debut du bloc pour remplacer enregistrement existant
                string v_bloc = v_ParentKeyName.Replace("\\", "");
                bool v_trouve = false;
                int v_index = v_fichierIni.FindIndex(x => (x == "[" + v_bloc + "]"));
                if (v_index >= 0)
                {
                    #region Bloc Trouve
                    v_index++;
                    while ((v_index < v_fichierIni.Count) && (!v_fichierIni[v_index].StartsWith("[")))
                    {
                        if (v_fichierIni[v_index].StartsWith(NomVariable + "="))
                        {
                            v_fichierIni[v_index] = NomVariable + "=" + Variable.ToString();
                            v_trouve = true;
                            break;
                        }
                        v_index++;
                    }
                    if (!v_trouve) //ajoute l'enregistrement
                    { v_fichierIni.Insert(v_index, NomVariable + "=" + Variable.ToString()); }
                    #endregion
                }
                else
                { //cree bloc et enregistrement
                    v_fichierIni.Add("[" + v_bloc + "]");
                    v_fichierIni.Insert(v_fichierIni.Count, NomVariable + "=" + Variable.ToString());
                }
                return true;
            }
            catch { return false; }
        }

        protected override string m_readInterne(string NomVariable)
        {
            try
            {
                string v_bloc = v_ParentKeyName.Replace("\\", "");
                int v_index = v_fichierIni.FindIndex(x => x == "[" + v_ParentKeyName.Replace("\\", "") + "]");
                if (v_index >= 0)
                {
                    v_index++;
                    while ((v_index < v_fichierIni.Count) && (!v_fichierIni[v_index].StartsWith("[")))
                    {
                        if (v_fichierIni[v_index].StartsWith(NomVariable + "="))
                        {
                            int v_indexSigneEgale = v_fichierIni[v_index].IndexOf('=');
                            return v_fichierIni[v_index].Substring(v_indexSigneEgale + 1);
                        }
                        v_index++;
                    }
                }
                return "";
            }
            catch { return ""; }
        }

        protected override void m_WriteIni()
        {
            //sauvegarde le fichier Ini
            StreamWriter sW = new StreamWriter(v_AdresseBase, false);
            foreach (var v_line in v_fichierIni)
            {
                sW.WriteLine(v_line);
            }
            sW.Close();
        }
    }
}
