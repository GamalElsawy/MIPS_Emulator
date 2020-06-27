using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MIPS_Emulator
{

    public partial class Form1 : Form
    {
        MIPS mipsObject;
        static int xDim = 0, yDim = 0,columnToDisplay = 0, gabCycles = 4,desiredJumpPC = -1,desiredLevel = -1;
        public class MIPS
        {

            int[] mipsReg;
            string[,] mipsTable;
            int AExcute, BExcute, ImmExcute, AOutMem, BMem, AOutWB,
                PC, PcFetch, PcDecode, PcMem, PcExcute;
            string IrDecode, DataMemRead;
           
            Dictionary<int,string> instructionSet;
            public MIPS()
            {
                PC = 1000;
                mipsTable = new string[100, 13];
                for (int i = 0; i < 100; i++)
                    for (int j = 0; j < 13; j++)
                        mipsTable[i, j] = "???";
                mipsReg = new int[32];
                mipsReg[0] = 0;
                for (int i = 1; i < 32; i++)
                    mipsReg[i] = i + 100;

                IrDecode = DataMemRead = "";
                AExcute = BExcute = ImmExcute = AOutMem = BMem = AOutWB  = 0 ;
                PcFetch = PcDecode = PcExcute = PcMem = 0;
               

            }
            public void update(DataGridView mipsRegGrid, TextBox pcTextBox)
            {
                mipsRegGrid.Rows.Clear();

                pcTextBox.Text = PC.ToString(); 
                for (int i = 0; i < 32; i++)
                {
                    string regNumber = '$' + i.ToString();
                    string[] newRow = new string[] { regNumber, mipsReg[i].ToString() };
                    mipsRegGrid.Rows.Add(newRow);
                }
            
            }

            string rs, rt;
            public void fetch()
            {
                PcDecode = PC + 4;
                IrDecode = instructionSet[PC];
                rs = peaceOfString(IrDecode, 7, 10);
                rt = peaceOfString(IrDecode, 11, 15);
                mipsTable[xDim, yDim] = PC.ToString(); //0
                mipsTable[++xDim, yDim] = PcDecode.ToString(); //1
                mipsTable[++xDim, yDim] = IrDecode; //2
                xDim++; yDim++;
            }
            public void decode()
            {

                PcExcute = PcDecode;
                int[] outOfRegFile = registerFile(binaryToDecimal(rs), binaryToDecimal(rt));
                AExcute = outOfRegFile[0];
                BExcute = outOfRegFile[1];
                ImmExcute = binaryToDecimal(peaceOfString(IrDecode, 16, 31));
                mipsTable[xDim, yDim] = PcExcute.ToString(); //3
                mipsTable[++xDim, yDim] = AExcute.ToString(); //4
                mipsTable[++xDim, yDim] = BExcute.ToString(); //5
                mipsTable[++xDim, yDim] = ImmExcute.ToString(); //6
                xDim++; yDim++;
            }

            public void excute()
            {
                BMem = BExcute;
                AOutMem = ALU(AExcute, BExcute, binaryToDecimal(peaceOfString(IrDecode, 26, 31)));
                mipsReg[binaryToDecimal(peaceOfString(IrDecode, 16, 20))] = AOutMem;
                PcMem = additionSubAdder(ImmExcute * 4, PcExcute, 0);
                mipsTable[xDim, yDim] = PcMem.ToString(); //7
                mipsTable[++xDim, yDim] = AOutMem.ToString(); //8
                mipsTable[++xDim, yDim] = BMem.ToString(); //9
                xDim++; yDim++;
            }

            public void memory()
            {
                AOutWB = AOutMem;
                DataMemRead = "??";
                mipsTable[xDim, yDim] = AOutWB.ToString(); //10
                mipsTable[++xDim, yDim] = DataMemRead;  //11
                xDim= 0; yDim -= 2;
            }

            public void fillInstructionSet(RichTextBox userCodeData)
            {
                instructionSet = new Dictionary<int, string>();
                int linesCount = userCodeData.Lines.Count();
                if (linesCount == 0)
                {
                    MessageBox.Show("Please Add Some Instructions And Try Again !!");
                    return;
                }

                for (int i = 0; i < linesCount; i++)
                {
                    string line = userCodeData.Lines[i], temp = "";
                    if (line.Trim().Length == 0) break;
                    for (int w = 0; w < line.Length; w++)
                        if (line[w] != ' ') temp += line[w];

                    string[] arr = temp.Split(':');
                    instructionSet[int.Parse(arr[0])] = arr[1];
                }
            }
            
            public void calculateCycles(RichTextBox userCodeData, DataGridView pipRegGrid, DataGridView mipsRegGrid, TextBox pcTextBox)
            {

                fillInstructionSet(userCodeData);
                
                if (!instructionSet.ContainsKey(PC) || instructionSet[PC] == "00000000000000000000000000000000")
                {
                    if (gabCycles == 0) return;
                    else
                    {
                        gabCycles--;
                        displayPiplineRegisters(pipRegGrid, columnToDisplay++);
                        return;
                    }
                }
                
                if (peaceOfString(instructionSet[PC], 0, 5) == "000100")
                {
                    PcDecode = PC + 4;
                    IrDecode = instructionSet[PC];
                    mipsTable[xDim, yDim] = PC.ToString(); //0
                    mipsTable[++xDim, yDim] = PcDecode.ToString(); //1
                    mipsTable[++xDim, yDim] = IrDecode; //2
                    
                    xDim = 0; yDim++;
                    displayPiplineRegisters(pipRegGrid, columnToDisplay++);
                    if (binaryToDecimal(peaceOfString(instructionSet[PC], 6, 10)) ==
                        binaryToDecimal(peaceOfString(instructionSet[PC], 11, 15)))
                    {
                        desiredJumpPC = PC +(binaryToDecimal(peaceOfString(instructionSet[PC], 16, 31)) * 4) + 4;
                        desiredLevel = 1;
                    }
                    
                    PC += 4;

                    return;
                }

                if(desiredJumpPC == PC || desiredJumpPC == -1)
                {
                    fetch();
                    decode();
                    excute();
                    memory();

                    desiredJumpPC = -1;
                    desiredLevel = -1;
                }
                else 
                {
                    if(desiredLevel == 1)
                    {
                        fetch();
                        decode();
                        excute();
                        xDim = 0;
                        yDim -= 2;
                       
                    }else if(desiredLevel == 2)
                    {
                        fetch();
                        decode();
                        xDim = 0;
                        yDim -= 1;
                    }
                    else if(desiredLevel == 3)
                    {
                        fetch();
                        xDim = 0;
                    }
                    else
                    {
                        PC = desiredJumpPC;
                        fetch();
                        decode();
                        excute();
                        memory();
                    }

                    desiredLevel++;
                }
                
                displayPiplineRegisters(pipRegGrid, columnToDisplay++);
                update(mipsRegGrid, pcTextBox);
                
                PC += 4;
                
            }
           
            public void displayPiplineRegisters(DataGridView pipRegGrid,int colToDis)
            {
                pipRegGrid.Rows.Clear();
                string[] newRow = new string[] { "PC_Fetch", mipsTable[0,colToDis] };
                pipRegGrid.Rows.Add(newRow);
                newRow = new string[] { "PC_Decode", mipsTable[1, colToDis] };
                pipRegGrid.Rows.Add(newRow);
                newRow = new string[] { "IR_Decode", mipsTable[2, colToDis] };
                pipRegGrid.Rows.Add(newRow);
                
                newRow = new string[] { "PC_Excute", mipsTable[3, colToDis] };
                pipRegGrid.Rows.Add(newRow);
                
                newRow = new string[] { "A_Excute", mipsTable[4, colToDis] };
                pipRegGrid.Rows.Add(newRow);
                newRow = new string[] { "B_Excute", mipsTable[5, colToDis] };
                pipRegGrid.Rows.Add(newRow);
                
                newRow = new string[] { "Imm_Excute", mipsTable[6, colToDis] };
                pipRegGrid.Rows.Add(newRow);

                newRow = new string[] { "PC_Mem", mipsTable[7, colToDis] };
                pipRegGrid.Rows.Add(newRow);
                newRow = new string[] { "A_Out_Mem", mipsTable[8, colToDis] };
                pipRegGrid.Rows.Add(newRow);
                newRow = new string[] { "B_Mem", mipsTable[9, colToDis] };
                pipRegGrid.Rows.Add(newRow);
 
                newRow = new string[] { "A_Out_WB", mipsTable[10, colToDis] };
                pipRegGrid.Rows.Add(newRow);
                newRow = new string[] { "Data_Mem_read", mipsTable[11, colToDis] };
                pipRegGrid.Rows.Add(newRow);
               
            }


            public int ALU(int in1, int in2, int op)
            {
                if (op == 36) return in1 & in2;
                else if (op == 37) return in1 | in2;
                else if (op == 32) return in1 + in2;
                else if (op == 34) return in1 - in2;
                else return -999999999;
            }
            public int mux2x1(int in1, int in2, int sel)
            {
                if (sel == 0) return in1;
                else if (sel == 1) return in2;
                else return -1;
            }
            public int additionSubAdder(int in1, int in2, int operationtype)
            {
                if (operationtype == 0) return in1 + in2;
                else if (operationtype == 1) return in1 - in2;
                else return -1;
            }

            public int mux4x1(int in1, int in2, int in3, int in4, int slctr)
            {
                if (slctr == 0) return in1;
                else if (slctr == 1) return in2;
                else if (slctr == 2) return in3;
                else if (slctr == 3) return in4;
                else return -1;
            }

            public int[] registerFile(int regIdx1, int regIdx2)
            {
                int[] returnData = new int[2];
                returnData[0] = mipsReg[regIdx1];
                returnData[1] = mipsReg[regIdx2];
                return returnData;
            }
            
            public int binaryToDecimal(string binaryCode)
            {
                return Convert.ToInt32(binaryCode, 2);
            }

            public string decimalToBinary(int decimalCode)
            {
                return Convert.ToString(decimalCode, 2);
            }
            public string peaceOfString(string str,int startIDX,int EndIDX)
            {
                if (str.Length < 32) return "0";
                string subStr = "";
                for (int i = startIDX; i <= EndIDX; i++)
                    subStr += str[i];
                return subStr;
            }

        }
        public Form1()
        {
            InitializeComponent();
            mipsObject = new MIPS();
            mipsObject.update(mipsRegGrid,pcTextBox);
        }

        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            mipsObject = new MIPS();
            mipsRegGrid.Rows.Clear();
            pipRegGrid.Rows.Clear();
            pcTextBox.Text = "1000";
            xDim = yDim = columnToDisplay = 0;
            gabCycles = 4;
            desiredJumpPC = desiredLevel = -1;
            mipsObject.fillInstructionSet(userCodeData);
            mipsObject.update(mipsRegGrid, pcTextBox);
            
        }
        
        private void pcBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void userCodeData_TextChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            mipsObject.calculateCycles(userCodeData,pipRegGrid,mipsRegGrid,pcTextBox);
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void mipsRegGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
