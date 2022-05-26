using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembler
{
    public class Assembler
    {
        private const int WORD_SIZE = 16;

        private Dictionary<string, int[]> m_dControl, m_dJmp, m_dDest; //these dictionaries map command mnemonics to machine code - they are initialized at the bottom of the class

        //more data structures here (symbol map, ...)
        private Dictionary<string, int> symbolsTable;
        private HashSet<string> labelsSet;
        private HashSet<string> labelsLineSet;

        public Assembler()
        {
            InitCommandDictionaries();
        }

        //this method is called from the outside to run the assembler translation
        public void TranslateAssemblyFile(string sInputAssemblyFile, string sOutputMachineCodeFile)
        {
            //read the raw input, including comments, errors, ...
            StreamReader sr = new StreamReader(sInputAssemblyFile);
            List<string> lLines = new List<string>();
            while (!sr.EndOfStream)
            {
                lLines.Add(sr.ReadLine());
            }
            sr.Close();
            //translate to machine code
            List<string> lTranslated = TranslateAssemblyFile(lLines);
            //write the output to the machine code file
            StreamWriter sw = new StreamWriter(sOutputMachineCodeFile);
            foreach (string sLine in lTranslated)
                sw.WriteLine(sLine);
            sw.Close();
        }

        //translate assembly into machine code
        private List<string> TranslateAssemblyFile(List<string> lLines)
        {
            //implementation order:
            //first, implement "TranslateAssemblyToMachineCode", and check if the examples "Add", "MaxL" are translated correctly.
            //next, implement "CreateSymbolTable", and modify the method "TranslateAssemblyToMachineCode" so it will support symbols (translating symbols to numbers). check this on the examples that don't contain macros
            //the last thing you need to do, is to implement "ExpendMacro", and test it on the example: "SquareMacro.asm".
            //init data structures here 

            //expand the macros
            List<string> lAfterMacroExpansion = ExpendMacros(lLines);

            //first pass - create symbol table and remove lable lines
            CreateSymbolTable(lAfterMacroExpansion);

            //second pass - replace symbols with numbers, and translate to machine code
            List<string> lAfterTranslation = TranslateAssemblyToMachineCode(lAfterMacroExpansion);
            return lAfterTranslation;
        }

        
        //first pass - replace all macros with real assembly
        private List<string> ExpendMacros(List<string> lLines)
        {
            //You do not need to change this function, you only need to implement the "ExapndMacro" method (that gets a single line == string)
            List<string> lAfterExpansion = new List<string>();
            for (int i = 0; i < lLines.Count; i++)
            {
                //remove all redudant characters
                string sLine = CleanWhiteSpacesAndComments(lLines[i]);
                if (sLine == "")
                    continue;
                //if the line contains a macro, expand it, otherwise the line remains the same
                List<string> lExpanded = ExapndMacro(sLine);
                //we may get multiple lines from a macro expansion
                foreach (string sExpanded in lExpanded)
                {
                    lAfterExpansion.Add(sExpanded);
                }
            }
            return lAfterExpansion;
        }

        //expand a single macro line
        private List<string> ExapndMacro(string sLine)
        {
            List<string> lExpanded = new List<string>();

            if (IsCCommand(sLine))
            {
                string sDest, sCompute, sJmp;
                GetCommandParts(sLine, out sDest, out sCompute, out sJmp);
                //your code here - check for indirect addessing and for jmp shortcuts
                //read the word file to see all the macros you need to support

                if (sCompute.Contains("++"))
                {
                    char plusChar = '+';
                    int plusIndex = sCompute.IndexOf(plusChar);
                    String dest = sCompute.Substring(0, plusIndex);

                    //X++
                    if (!m_dDest.ContainsKey(dest))
                    {
                        
                        String line1 = "@" + dest;
                        String line2 = "M=M+1";

                        lExpanded.Add(line1);
                        lExpanded.Add(line2);
                    }
                    //D++
                    else
                    {
                        string line = sDest + "=" + sDest + "+1";
                        lExpanded.Add(line);
                    }
                    
                                         
                }

                if (sCompute.Contains("--"))
                {
                    char minusChar = '-';
                    int minusIndex = sCompute.IndexOf(minusChar);
                    String dest = sCompute.Substring(0, minusIndex);
                    //X--
                    if (!m_dDest.ContainsKey(dest))
                    {
                        
                        String line1 = "@" + dest;
                        String line2 = "M=M-1";

                        lExpanded.Add(line1);
                        lExpanded.Add(line2);
                    }
                    //D--
                    else
                    {
                        string line = sDest + "=" + sDest + "-1";
                        lExpanded.Add(line);
                    }


                }
                //X=Y
                else if (!m_dControl.ContainsKey(sCompute) && !m_dDest.ContainsKey(sDest))
                {
                    String line1 = "@" + sCompute;
                    String line2 = "D=M";
                    String line3 = "@" + sDest;
                    String line4 = "M=D";

                    lExpanded.Add(line1);
                    lExpanded.Add(line2);
                    lExpanded.Add(line3);
                    lExpanded.Add(line4);
                }
                //X=D
                else if (m_dControl.ContainsKey(sCompute) && !m_dDest.ContainsKey(sDest))
                {
                    String line1 = "@" + sDest;
                    String line2 = "M=" + sCompute;

                    lExpanded.Add(line1);
                    lExpanded.Add(line2);
                }
                //D=X
                else if (!m_dControl.ContainsKey(sCompute) && m_dDest.ContainsKey(sDest))
                {
                    String line1 = "@" + sCompute;
                    String line2 = sDest + "=M";

                    lExpanded.Add(line1);
                    lExpanded.Add(line2);
                }

                else if (int.TryParse(sCompute, out int num) && sLine.Contains('=') && !m_dControl.ContainsKey(sCompute))
                {
                    //D=5
                    if (m_dDest.ContainsKey(sDest))
                    {
                        String line1 = "@" + sCompute;
                        String line2 = sDest + "=A";

                        lExpanded.Add(line1);
                        lExpanded.Add(line2);
                    }
                    //X=5
                    else
                    {
                        String line1 = "@" + sCompute;
                        String line2 = "D=A";
                        String line3 = "@" + sDest;
                        String line4 = "M=D";

                        lExpanded.Add(line1);
                        lExpanded.Add(line2);
                        lExpanded.Add(line3);
                        lExpanded.Add(line4);
                    }
                }
                //dest;jmp:line
                else if (sLine.Contains(':'))
                {
                    int idx = sJmp.IndexOf(':');
                    String jump = sJmp.Substring(0, idx);
                    String label = sJmp.Substring(idx + 1);

                    String line1 = "@" + label;
                    String line2 = sCompute + ";" + jump;

                    lExpanded.Add(line1);
                    lExpanded.Add(line2);
                }



            }
            if (lExpanded.Count == 0)
                lExpanded.Add(sLine);
            return lExpanded;
        }

        //second pass - record all symbols - labels and variables
        private void CreateSymbolTable(List<string> lLines)
        {
            string sLine = "";
            string tempString = "";
            int lineCounter = 0;
            int aCommandLabels = 16;
            labelsSet = new HashSet<string>();
            labelsLineSet = new HashSet<string>();

            symbolsTable = new Dictionary<string, int>();

            symbolsTable["R0"] = 0;
            symbolsTable["R1"] = 1;
            symbolsTable["R2"] = 2;
            symbolsTable["R3"] = 3;
            symbolsTable["R4"] = 4;
            symbolsTable["R5"] = 5;
            symbolsTable["R6"] = 6;
            symbolsTable["R7"] = 7;
            symbolsTable["R8"] = 8;
            symbolsTable["R9"] = 9;
            symbolsTable["R10"] = 10;
            symbolsTable["R11"] = 11;
            symbolsTable["R12"] = 12;
            symbolsTable["R13"] = 13;
            symbolsTable["R14"] = 14;
            symbolsTable["R15"] = 15;

            for (int i = 0; i < lLines.Count; i++)
            {
                sLine = lLines[i];
                
                if (IsLabelLine(sLine))
                {
                    //record label in symbol table
                    //do not add the label line to the result
                    tempString = sLine.Substring(1, sLine.Length - 2);
                    if (tempString[0] >= '0' && tempString[0] <= '9')
                    {
                        throw new AssemblerException(i, sLine, "Label can't start with a number");
                        //continue;
                    }
                    else if (labelsLineSet.Contains(tempString))
                    {
                        throw new AssemblerException(i, sLine, "This label is already exist");
                        //continue;
                    }

                    else
                    {
                        labelsLineSet.Add(tempString);
                        labelsSet.Add(tempString);
                        symbolsTable[tempString] = lineCounter;
                        continue;
                    }

                }
                else if (IsACommand(sLine))
                {
                    //may contain a variable - if so, record it to the symbol table (if it doesn't exist there yet...)
                    tempString = sLine.Substring(1);
                    bool isNumeric = int.TryParse(tempString, out int num);
                    if (isNumeric == false)
                    {
                        if (tempString[0] >= '0' && tempString[0] <= '9')
                        {
                            throw new AssemblerException(i, sLine, "Label can't start with a number");
                            //continue;
                        }
                        if (!labelsSet.Contains(tempString) && !labelsLineSet.Contains(tempString))
                        {
                            labelsSet.Add(tempString);
                            symbolsTable[tempString] = aCommandLabels;
                            aCommandLabels++;
                        }
                            
                    }
                    else if (tempString[0] == '-')
                    {
                        throw new AssemblerException(i, sLine, "Number of line can't be negative");
                    }

                }
                else if (IsCCommand(sLine))
                {
                    //do nothing here
                }
                else
                    throw new FormatException("Cannot parse line " + i + ": " + lLines[i]);

                lineCounter++;
            }
            lineCounter = 16;
            foreach (string s in labelsSet)
            {
                if (symbolsTable[s] == -1)
                {
                    symbolsTable[s] = lineCounter;
                    lineCounter++;
                }
            }


        }

        //third pass - translate lines into machine code, replacing symbols with numbers
        private List<string> TranslateAssemblyToMachineCode(List<string> lLines)
        {
            string sLine = "";
            List<string> lAfterPass = new List<string>();
            for (int i = 0; i < lLines.Count; i++)
            {
                sLine = lLines[i];

                if (IsACommand(sLine))
                {
                    //translate an A command into a sequence of bits
                    sLine = sLine.Substring(1);
                    bool isNumeric = int.TryParse(sLine, out int num);
                    if (isNumeric == false)
                    {
                        int labelNum = symbolsTable[sLine];
                        lAfterPass.Add(ToBinary(labelNum));

                        //if (!labelsSet.Contains(sLine))
                        //{
                          //  labelsSet.Add(sLine);
                            //symbolsTable[sLine] = -1;
                        //}
                    }
                    else
                    {
                        int binarryNumber = Int32.Parse(sLine);
                        if (binarryNumber > 32767)
                        {
                            throw new AssemblerException(i, sLine, "the number is too big..");
                        }
                        lAfterPass.Add(ToBinary(binarryNumber));
                    }


                    
                    
                }
                else if (IsCCommand(sLine))
                {
                    string sDest, sControl, sJmp;
                    GetCommandParts(sLine, out sDest, out sControl, out sJmp);
                    //translate an C command into a sequence of bits
                    //take a look at the dictionaries m_dControl, m_dJmp, and where they are initialized (InitCommandDictionaries), to understand how to you them here
                    int[] dest = m_dDest[sDest];
                    int[] control = m_dControl[sControl];
                    int[] jump = m_dJmp[sJmp];
                    //reverse the arrays
                    dest = GetArray(dest);
                    control = GetArray(control);
                    jump = GetArray(jump);

                    String cComand = "1000" + ToString(control) + ToString(dest) + ToString(jump);

                    lAfterPass.Add(cComand);
                }
                else if (IsLabelLine(sLine))
                {

                }
                else
                    throw new FormatException("Cannot parse line " + i + ": " + lLines[i]);
            }
            return lAfterPass;
        }

        //helper functions for translating numbers or bits into strings
        private string ToString(int[] aBits)
        {
            string sBinary = "";
            for (int i = 0; i < aBits.Length; i++)
                sBinary += aBits[i];
            return sBinary;
        }

        private string ToBinary(int x)
        {
            string sBinary = "";
            for (int i = 0; i < WORD_SIZE; i++)
            {
                sBinary = (x % 2) + sBinary;
                x = x / 2;
            }
            return sBinary;
        }


        //helper function for splitting the various fields of a C command
        private void GetCommandParts(string sLine, out string sDest, out string sControl, out string sJmp)
        {
            if (sLine.Contains('='))
            {
                int idx = sLine.IndexOf('=');
                sDest = sLine.Substring(0, idx);
                sLine = sLine.Substring(idx + 1);
            }
            else
                sDest = "";
            if (sLine.Contains(';'))
            {
                int idx = sLine.IndexOf(';');
                sControl = sLine.Substring(0, idx);
                sJmp = sLine.Substring(idx + 1);

            }
            else
            {
                sControl = sLine;
                sJmp = "";
            }
        }

        private bool IsCCommand(string sLine)
        {
            return !IsLabelLine(sLine) && sLine[0] != '@';
        }

        private bool IsACommand(string sLine)
        {
            return sLine[0] == '@';
        }

        private bool IsLabelLine(string sLine)
        {
            if (sLine.StartsWith("(") && sLine.EndsWith(")"))
                return true;
            return false;
        }

        private string CleanWhiteSpacesAndComments(string sDirty)
        {
            string sClean = "";
            for (int i = 0 ; i < sDirty.Length ; i++)
            {
                char c = sDirty[i];
                if (c == '/' && i < sDirty.Length - 1 && sDirty[i + 1] == '/') // this is a comment
                    return sClean;
                if (c > ' ' && c <= '~')//ignore white spaces
                    sClean += c;
            }
            return sClean;
        }

        public int[] GetArray(params int[] l)
        {
            int[] a = new int[l.Length];
            for (int i = 0; i < l.Length; i++)
                a[l.Length - i - 1] = l[i];
            return a;
        }
        private void InitCommandDictionaries()
        {
            m_dControl = new Dictionary<string, int[]>();


            m_dControl["0"] = GetArray(0, 0, 0, 0, 0, 0);
            m_dControl["1"] = GetArray(0, 0, 0, 0, 0, 1);
            m_dControl["D"] = GetArray(0, 0, 0, 0, 1, 0);
            m_dControl["A"] = GetArray(0, 0, 0, 0, 1, 1);
            m_dControl["!D"] = GetArray(0, 0, 0, 1, 0, 0);
            m_dControl["!A"] = GetArray(0, 0, 0, 1, 0, 1);
            m_dControl["-D"] = GetArray(0, 0, 0, 1, 1, 0);
            m_dControl["-A"] = GetArray(0, 0, 0, 1, 1, 1);
            m_dControl["D+1"] = GetArray(0, 0, 1, 0, 0, 0);
            m_dControl["A+1"] = GetArray(0, 0, 1, 0, 0, 1);
            m_dControl["D-1"] = GetArray(0, 0, 1, 0, 1, 0);
            m_dControl["A-1"] = GetArray(0, 0, 1, 0, 1, 1);
            m_dControl["A+D"] = GetArray(0, 0, 1, 1, 0, 0);
            m_dControl["D+A"] = GetArray(0, 0, 1, 1, 0, 0);
            m_dControl["D-A"] = GetArray(0, 0, 1, 1, 0, 1);
            m_dControl["A-D"] = GetArray(0, 0, 1, 1, 1, 0);
            m_dControl["A^D"] = GetArray(0, 0, 1, 1, 1, 1);
            m_dControl["A&D"] = GetArray(0, 1, 0, 0, 0, 0);
            m_dControl["AvD"] = GetArray(0, 1, 0, 0, 0, 1);
            m_dControl["A|D"] = GetArray(0, 1, 0, 0, 1, 0);

            m_dControl["M"] = GetArray(1, 0, 0, 0, 1, 1);
            m_dControl["!M"] = GetArray(1, 0, 0, 1, 0, 1);
            m_dControl["-M"] = GetArray(1, 0, 0, 1, 1, 1);
            m_dControl["M+1"] = GetArray(1, 0, 1, 0, 0, 1);
            m_dControl["M-1"] = GetArray(1, 0, 1, 0, 1, 1);
            m_dControl["M+D"] = GetArray(1, 0, 1, 1, 0, 0);
            m_dControl["D+M"] = GetArray(1, 0, 1, 1, 0, 0);
            m_dControl["D-M"] = GetArray(1, 0, 1, 1, 0, 1);
            m_dControl["M-D"] = GetArray(1, 0, 1, 1, 1, 0);
            m_dControl["M^D"] = GetArray(1, 0, 1, 1, 1, 1);
            m_dControl["M&D"] = GetArray(1, 1, 0, 0, 0, 0);
            m_dControl["MvD"] = GetArray(1, 1, 0, 0, 0, 1);
            m_dControl["M|D"] = GetArray(1, 1, 0, 0, 1, 0);



            m_dDest = new Dictionary<string, int[]>();
            m_dDest[""] = GetArray(0, 0, 0);
            m_dDest["M"] = GetArray(0, 0, 1);
            m_dDest["D"] = GetArray(0, 1, 0);
            m_dDest["A"] = GetArray(1, 0, 0);
            m_dDest["DM"] = GetArray(0, 1, 1);
            m_dDest["AM"] = GetArray(1, 0, 1);
            m_dDest["AD"] = GetArray(1, 1, 0);
            m_dDest["ADM"] = GetArray(1, 1, 1);


            m_dJmp = new Dictionary<string, int[]>();

            m_dJmp[""] = GetArray(0, 0, 0);
            m_dJmp["JGT"] = GetArray(0, 0, 1);
            m_dJmp["JEQ"] = GetArray(0, 1, 0);
            m_dJmp["JGE"] = GetArray(0, 1, 1);
            m_dJmp["JLT"] = GetArray(1, 0, 0);
            m_dJmp["JNE"] = GetArray(1, 0, 1);
            m_dJmp["JLE"] = GetArray(1, 1, 0);
            m_dJmp["JMP"] = GetArray(1, 1, 1);
        }
    }
}
