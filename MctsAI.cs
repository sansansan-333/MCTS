using System;

namespace Mcts{
    public class MctsAI{

        private Mcts mcts;

        public MctsAI(){
            mcts = new Mcts(Node.UCT);
        }

        public Move Think(GameInfo gameInfo, int iterate_count){
            mcts.SetRoot(gameInfo);

            for(int i = 0; i < iterate_count; i++){
                mcts.RunAllSteps();
            }

            // Debug();

            return mcts.GetNextMove();
        }

        private void Debug(){
            string line = "";
            Node node = mcts.Root;
            while(line != "q"){
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.Write("\nCurrent depth is {0}. Input command: ", node.depth);
                Console.ResetColor();

                line = Console.ReadLine();
                switch(line){
                    case "q":
                        break;
                    case "p": // parent
                        if(!node.IsRoot()){
                            node = node.Parent;
                            node.PrintNode("------------------------------");
                        }
                        break;
                    case "c": // child
                        Console.WriteLine("There is/are {0} children. Input child number: ", node.Children.Count);
                        string a = Console.ReadLine();
                        if(Int32.TryParse(a, out int j) && j < node.Children.Count){
                            node = node.Children[j];
                            node.PrintNode("------------------------------");
                        }else if(a == "all"){
                            for(int i = 0; i < node.Children.Count; i++){
                                node.Children[i].PrintNode("---------------{" + i + "}---------------");
                            }
                        }
                        break;
                    case ".":
                        node.PrintNode("------------------------------");
                        break;
                    default:
                        break;
                }
            }
        }
    }
}