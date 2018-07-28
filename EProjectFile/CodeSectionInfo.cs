﻿using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;

namespace QIQI.EProjectFile
{
    public class CodeSectionInfo : IToTextCodeAble
    {
        public const string SectionName = "程序段";
        public const int SectionKey = 0x03007319;
        private int allocatedIdNum;
        [JsonIgnore]
        public byte[] UnknownBeforeLibrary_1 { get; set; }
        [JsonIgnore]
        public byte[] UnknownBeforeLibrary_2 { get; set; }
        [JsonIgnore]
        public byte[] UnknownBeforeLibrary_3 { get; set; }
        public LibraryRefInfo[] Libraries { get; set; }
        public int Flag { get; set; }
        /// <summary>
        /// 【仅用于不带有编辑信息的EC文件】“_启动子程序”，系统将在 初始模块段 保存的方法被调用完成后调用
        /// </summary>
        public int MainMethod { get; set; }
        [JsonIgnore]
        public byte[] UnknownBeforeIconData { get; set; }
        public byte[] IconData { get; set; }
        public string DebugCommandParameters { get; set; }
        public ClassInfo[] Classes { get; set; }
        public MethodInfo[] Methods { get; set; }
        public GlobalVariableInfo[] GlobalVariables { get; set; }
        public StructInfo[] Structs { get; set; }
        public DllDeclareInfo[] DllDeclares { get; set; }

        public static CodeSectionInfo Parse(byte[] data, bool cryptEc = false)
        {
            var codeSectionInfo = new CodeSectionInfo();
            using (var reader = new BinaryReader(new MemoryStream(data, false)))
            {
                codeSectionInfo.allocatedIdNum = reader.ReadInt32();
                reader.ReadInt32(); // 确认于易语言V5.71
                codeSectionInfo.UnknownBeforeLibrary_1 = reader.ReadBytesWithLengthPrefix(); // Unknown
                if (cryptEc)
                {
                    reader.ReadInt32();
                    reader.ReadInt32();
                    codeSectionInfo.UnknownBeforeLibrary_2 = reader.ReadBytesWithLengthPrefix(); // Unknown
                    codeSectionInfo.Flag = reader.ReadInt32();
                    codeSectionInfo.MainMethod = reader.ReadInt32();
                    codeSectionInfo.Libraries = LibraryRefInfo.ReadLibraries(reader);
                    codeSectionInfo.UnknownBeforeLibrary_3 = reader.ReadBytesWithLengthPrefix(); // Unknown
                }
                else
                {
                    codeSectionInfo.UnknownBeforeLibrary_2 = reader.ReadBytesWithLengthPrefix(); // Unknown
                    codeSectionInfo.UnknownBeforeLibrary_3 = reader.ReadBytesWithLengthPrefix(); // Unknown
                    codeSectionInfo.Libraries = LibraryRefInfo.ReadLibraries(reader);
                    codeSectionInfo.Flag = reader.ReadInt32();
                    codeSectionInfo.MainMethod = reader.ReadInt32();
                }

                if ((codeSectionInfo.Flag & 1) != 0)
                {
                    codeSectionInfo.UnknownBeforeIconData = reader.ReadBytes(16); // Unknown
                }
                codeSectionInfo.IconData = reader.ReadBytesWithLengthPrefix();
                codeSectionInfo.DebugCommandParameters = reader.ReadStringWithLengthPrefix();
                if (cryptEc)
                {
                    reader.ReadBytes(12);
                    codeSectionInfo.Methods = MethodInfo.ReadMethods(reader);
                    codeSectionInfo.DllDeclares = DllDeclareInfo.ReadDllDeclares(reader);
                    codeSectionInfo.GlobalVariables = AbstractVariableInfo.ReadVariables(reader, x => new GlobalVariableInfo(x));
                    codeSectionInfo.Classes = ClassInfo.ReadClasses(reader);
                    codeSectionInfo.Structs = StructInfo.ReadStructs(reader);
                }
                else
                {
                    codeSectionInfo.Classes = ClassInfo.ReadClasses(reader);
                    codeSectionInfo.Methods = MethodInfo.ReadMethods(reader);
                    codeSectionInfo.GlobalVariables = AbstractVariableInfo.ReadVariables(reader, x => new GlobalVariableInfo(x));
                    codeSectionInfo.Structs = StructInfo.ReadStructs(reader);
                    codeSectionInfo.DllDeclares = DllDeclareInfo.ReadDllDeclares(reader);
                }
            }
            return codeSectionInfo;
        }
        /// <summary>
        /// 分配一个Id
        /// </summary>
        /// <param name="type">参考EplSystemId.Type_***</param>
        /// <returns>指定类型的新Id</returns>
        public int AllocId(int type)
        {
            return ++allocatedIdNum | type;
        }
        public byte[] ToBytes()
        {
            byte[] data;
            using (var writer = new BinaryWriter(new MemoryStream()))
            {
                WriteTo(writer);
                writer.Flush();
                data = ((MemoryStream)writer.BaseStream).ToArray();
            }
            return data;
        }
        private void WriteTo(BinaryWriter writer)
        {
            writer.Write(allocatedIdNum);
            writer.Write(51113791); // 确认于易语言V5.71
            writer.WriteBytesWithLengthPrefix(UnknownBeforeLibrary_1);
            writer.WriteBytesWithLengthPrefix(UnknownBeforeLibrary_2);
            writer.WriteBytesWithLengthPrefix(UnknownBeforeLibrary_3);
            LibraryRefInfo.WriteLibraries(writer, Libraries);
            writer.Write(Flag);
            writer.Write(MainMethod);
            if (UnknownBeforeIconData != null)
            {
                writer.WriteBytesWithLengthPrefix(UnknownBeforeIconData);
            }
            writer.WriteBytesWithLengthPrefix(IconData);
            writer.WriteStringWithLengthPrefix(DebugCommandParameters);
            ClassInfo.WriteClasses(writer, Classes);
            MethodInfo.WriteMethods(writer, Methods);
            AbstractVariableInfo.WriteVariables(writer, GlobalVariables);
            StructInfo.WriteStructs(writer, Structs);
            DllDeclareInfo.WriteDllDeclares(writer, DllDeclares);
            writer.Write(new byte[40]); // Unknown（40个0）
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
        public void ToTextCode(IdToNameMap nameMap, StringBuilder result, int indent = 0)
        {
            ToTextCode(nameMap, result, indent, true);
        }
        public void ToTextCode(IdToNameMap nameMap, StringBuilder result, int indent, bool writeMethod, bool writeCode = true)
        {
            if (GlobalVariables != null && GlobalVariables.Length != 0)
            {
                TextCodeUtils.WriteJoinCode(GlobalVariables, Environment.NewLine, nameMap, result, indent);
                result.AppendLine();
            }
            TextCodeUtils.WriteJoinCode(Classes, Environment.NewLine, writeMethod ? this : null, nameMap, result, indent, writeCode);
            if (DllDeclares != null && DllDeclares.Length != 0)
            {
                result.AppendLine();
                TextCodeUtils.WriteJoinCode(DllDeclares, Environment.NewLine, nameMap, result, indent);
            }
            if (Structs != null && Structs.Length != 0)
            {
                result.AppendLine();
                TextCodeUtils.WriteJoinCode(Structs, Environment.NewLine, nameMap, result, indent);
            }
        }
    }
}