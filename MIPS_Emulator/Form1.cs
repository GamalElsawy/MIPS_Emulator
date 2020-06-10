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
        static int i = 0, j = 0,columnToDisplay = 0, gabCycles = 4,desiredJumpPC = -1,desiredlevel = -1;
        public class MIPS
        {

            int[] mipsReg;
            string[,] mipsTable;
            int A_Excute, B_Excute, Imm_Excute, A_Out_Mem, B_Mem, A_Out_WB,
                PC, PC_Fetch, PC_Decode, PC_Mem, PC_Excute;
            string IR_Decode, Data_Mem_read;
           
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

                IR_Decode = Data_Mem_read = "";
                A_Excute = B_Excute = Imm_Excute = A_Out_Mem = B_Mem = A_Out_WB  = 0 ;
                PC_Fetch = PC_Decode = PC_Excute = PC_Mem = 0;
               

            }
            public void Update(DataGridView mipsRegGrid, TextBox pcTextBox)
            {
                mipsRegGrid.Rows.Clear();
                //pipRegGrid.Rows.Clear();

                pcTextBox.Text = PC.ToString(); 
                for (int i = 0; i < 32; i++)
                {
                    string regNumber = '$' + i.ToString();
                    string[] newRow = new string[] { regNumber, mipsReg[i].ToString() };
                    mipsRegGrid.Rows.Add(newRow);
                }
            
            }

            string s1, s2;
            public void fetch()
            {
                PC_Decode = PC + 4;
                IR_Decode = instructionSet[PC];
                s1 = peaceOfString(IR_Decode, 7, 10);
                s2 = peaceOfString(IR_Decode, 11, 15);
                mipsTable[i, j] = PC.ToString(); //0
                mipsTable[++i, j] = PC_Decode.ToString(); //1
                mipsTable[++i, j] = IR_Decode; //2
                i++; j++;
            }
            public void decode()
            {

                PC_Excute = PC_Decode;
                int[] outOfRegFile = registerFile(binaryToDecimal(s1), binaryToDecimal(s2));
                A_Excute = outOfRegFile[0];
                B_Excute = outOfRegFile[1];
                Imm_Excute = binaryToDecimal(peaceOfString(IR_Decode, 16, 31));
                mipsTable[i, j] = PC_Excute.ToString(); //3
                mipsTable[++i, j] = A_Excute.ToString(); //4
                mipsTable[++i, j] = B_Excute.ToString(); //5
                mipsTable[++i, j] = Imm_Excute.ToString(); //6
                i++; j++;
            }

            public void excute()
            {
                B_Mem = B_Excute;
                A_Out_Mem = ALU(A_Excute, B_Excute, binaryToDecimal(peaceOfString(IR_Decode, 26, 31)));
                mipsReg[binaryToDecimal(peaceOfString(IR_Decode, 16, 20))] = A_Out_Mem;
                PC_Mem = additionSubAdder(Imm_Excute * 4, PC_Excute, 0);
                mipsTable[i, j] = PC_Mem.ToString(); //7
                mipsTable[++i, j] = A_Out_Mem.ToString(); //8
                mipsTable[++i, j] = B_Mem.ToString(); //9
                i++; j++;
            }

            public void memory()
            {
                A_Out_WB = A_Out_Mem;
                Data_Mem_read = "??";
                mipsTable[i, j] = A_Out_WB.ToString(); //10
                mipsTable[++i, j] = Data_Mem_read;  //11
                i = 0; j -= 2;
            }

            public void getInstructionSet(RichTextBox userCodeData)
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
                
                getInstructionSet(userCodeData);
                
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
                    PC_Decode = PC + 4;
                    IR_Decode = instructionSet[PC];
                    mipsTable[i, j] = PC.ToString(); //0
                    mipsTable[++i, j] = PC_Decode.ToString(); //1
                    mipsTable[++i, j] = IR_Decode; //2
                    
                    i = 0; j++;
                    displayPiplineRegisters(pipRegGrid, columnToDisplay++);
                    if (binaryToDecimal(peaceOfString(instructionSet[PC], 6, 10)) ==
                        binaryToDecimal(peaceOfString(instructionSet[PC], 11, 15)))
                    {
                        desiredJumpPC = PC +(binaryToDecimal(peaceOfString(instructionSet[PC], 16, 31)) * 4) + 4;
                        desiredlevel = 1;
                        //MessageBox.Show("desiredJumpPC: " + desiredJumpPC.ToString());
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
                    desiredlevel = -1;
                    //MessageBox.Show("in Desired");
                }
                else 
                {
                    if(desiredlevel == 1)
                    {
                        fetch();
                        decode();
                        excute();
                        i = 0;
                        j -= 2;
                        //MessageBox.Show("FDE");
                       
                    }else if(desiredlevel == 2)
                    {
                        fetch();
                        decode();
                        i = 0;
                        j -= 1;
                        //MessageBox.Show("FD");
                    }
                    else if(desiredlevel == 3)
                    {
                        fetch();
                        i = 0;
                        //MessageBox.Show("F");
                        
                    }
                    else
                    {
                        //MessageBox.Show("J to desired");
                        PC = desiredJumpPC;
                        fetch();
                        decode();
                        excute();
                        memory();
                    }

                    desiredlevel++;
                }
                
                displayPiplineRegisters(pipRegGrid, columnToDisplay++);
                Update(mipsRegGrid, pcTextBox);
                
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
                //MessageBox.Show(op.ToString());
                if (op == 36) return in1 & in2;
                else if (op == 37) return in1 | in2;
                else if (op == 32) return in1 + in2;
                else if (op == 34) return in1 - in2;
                else return -999999999;
            }
            public int mux_2x1(int in1, int in2, int sel)
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

            public int mux_4x1(int in1, int in2, int in3, int in4, int slctr)
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
                //MessageBox.Show(returnData[0].ToString() + " " + returnData[1].ToString());
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
            mipsObject.Update(mipsRegGrid,pcTextBox);
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
            i = j = columnToDisplay = 0;
            gabCycles = 4;
            desiredJumpPC = desiredlevel = -1;
            mipsObject.getInstructionSet(userCodeData);
            mipsObject.Update(mipsRegGrid, pcTextBox);
            
        }
        
        private void pcBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void userCodeData_TextChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            //mipsObject.getInstructionSet(userCodeData);
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
