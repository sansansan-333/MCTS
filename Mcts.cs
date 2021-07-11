using System;
using System.Collections.Generic;

namespace Mcts
{
    public class Mcts
    {
        private readonly int EXPANDER_THRESHOLD = 10;
        public Node Root{ private set; get; }
        private int totalPlayout;
        private string selectionFormula;

        public Mcts(string selectionFormula){
            this.selectionFormula = selectionFormula;
            totalPlayout = 0;
        }

        public void SetRoot(GameInfo gameInfo){
            Root = new Node(gameInfo);
            Root.depth = 0;
            Root.SetSelectionFormula(selectionFormula);
            Root.SetPossibleChildren();
        }

        public void RunAllSteps(){
            Select();
            bool expanded = Expand();
            if(expanded){
                float playout_result = Simulate();
                BackPropagate(playout_result);
            }
        }

        private Node target_leaf; // used among four steps below

        private void Select(){
            Node child = Root;
            while(!child.IsLeaf()){
                child = child.SelectChild(totalPlayout, Root.GameInfo);
            }
            target_leaf = child;
        }

        private bool Expand(){
            if(target_leaf.PlayoutCount > EXPANDER_THRESHOLD && target_leaf.IsExpandable()){
                if(target_leaf.SetPossibleChildren() == false){
                    return false;
                }
                target_leaf = target_leaf.SelectChild(totalPlayout, Root.GameInfo);
            }
            return true;
        }

        private float Simulate(){
            totalPlayout++;
            return target_leaf.Playout();
        }

        private void BackPropagate(float playout_result){
            Node updating_node = target_leaf;

            while(true){
                updating_node.UpdateInfo(playout_result);
                playout_result = 1 - playout_result;
                if(updating_node.IsRoot()){
                    break;
                }
                updating_node = updating_node.Parent;
            }
        }

        public Move GetNextMove(){
            string pre_selection_formula = Root.SelectionFormula;
            var next_node = Root.SelectChild(totalPlayout, Node.WIN_RATE, Root.GameInfo);
            Root.SetSelectionFormula(pre_selection_formula);
            return next_node.GameInfo.PreMove;
        }
    }

    public class Node{
        public int PlayoutCount{ private set; get;}
        public float WinCount{ private set; get; }

        public Node Parent{ private set; get; }
        public List<Node> Children{ private set; get; }

        public string SelectionFormula{ private set; get; }
        static public readonly string UCT = nameof(UCT);
        static public readonly string WIN_RATE = nameof(WIN_RATE);
        public GameInfo GameInfo{ private set; get; }

        public int depth;

        public Node(){
            PlayoutCount=0;
            Children = new List<Node>();
        }

        public Node(GameInfo gameInfo){
            this.GameInfo = gameInfo;
            PlayoutCount=0;
            Children = new List<Node>();
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="node"></param>
        public Node(Node node){
            PlayoutCount = node.PlayoutCount;
            WinCount = node.WinCount;
            Parent = node.Parent;
            Children = node.Children;
            SelectionFormula = node.SelectionFormula;
            GameInfo = node.GameInfo;
            depth = node.depth;
        }

        /// <summary>
        /// Reuturn true if this node is a root.
        /// </summary>
        /// <returns></returns>
        public bool IsRoot(){
            return Parent == null;
        }

        /// <summary>
        /// Return true if this node is a leaf.
        /// </summary>
        /// <returns></returns>        
        public bool IsLeaf(){
            return Children.Count == 0;
        }

        /// <summary>
        /// Return false if there is already winner in this node
        /// </summary>
        /// <returns></returns>
        public bool IsExpandable(){
            return GameInfo.IsExpandable();
        }

        /// <summary>
        /// Set the formula such as UCT that are used for selecting a child node.
        /// </summary>
        /// <param name="selectionFormula"></param>
        public void SetSelectionFormula(string selectionFormula){
            this.SelectionFormula = selectionFormula;
        }
 
        /// <summary>
        /// Select a child by using selection formula.
        /// </summary>
        /// <param name="totalPlayout"></param>
        /// <returns></returns>
        public Node SelectChild(int totalPlayout, GameInfo root_gameInfo){
            if(SelectionFormula == null){
                SelectionFormula = Parent.SelectionFormula;
            }

            bool same_player = GameInfo.IsSamePlayer(this.GameInfo, root_gameInfo); // true if root player is the same to this node's player

            if(SelectionFormula == UCT){
                Node chosen_child = new Node();
                float maxUCT = float.MinValue;
                foreach(var child in Children){
                    float UCT = child.GetUCT(totalPlayout, root_gameInfo);
                    if(UCT > maxUCT){
                        chosen_child = child;
                        maxUCT = UCT;
                    }
                }
                return chosen_child;
            }else if(SelectionFormula == WIN_RATE){
                Node chosen_child = new Node();
                float minRate = float.MaxValue, win_rate;
                foreach(var child in Children){
                    win_rate = child.GetWinRate();
                    if(minRate > win_rate){
                        chosen_child = child;
                        minRate = win_rate;
                    }
                }
                return chosen_child;
            }else{
                Console.Error.WriteLine(nameof(SelectChild) + ": invalid selectionFormula.");
                return null;
            }
        }

        /// <summary>
        /// Set the formula and then select a child by using selection formula.
        /// </summary>
        /// <param name="totalPlayout"></param>
        /// <param name="selectionFormula"></param>
        /// <returns></returns>
        public Node SelectChild(int totalPlayout, string selectionFormula, GameInfo root_gameInfo){
            this.SelectionFormula = selectionFormula;
            return SelectChild(totalPlayout, root_gameInfo);
        }

        
        /// <summary>
        /// Set all possible children to this node.
        /// </summary>
        /// <returns>false if no child node is found</returns>
        public bool SetPossibleChildren(){
            var children = GameInfo.GetPossibleChildren();
            if(children.Count == 0) return false;

            Children.AddRange(children);
            foreach(var child in Children){
                child.Parent = this;
                child.depth = this.depth + 1;
            }

            return true;
        }

        /// <summary>
        /// Perform a playout and return the result.
        /// </summary>
        /// <returns></returns>
        public float Playout(){
            return GameInfo.Playout();
        }

        /// <summary>
        /// Update information about the number of wins and playouts.
        /// </summary>
        /// <param name="is_win"></param>
        public void UpdateInfo(float playout_result){
            PlayoutCount++;
            WinCount += playout_result;
        }

        /// <summary>
        /// Calculate UCT.
        /// </summary>
        /// <remarks>
        /// This method may return a random value, 
        /// which means every time you call, different value may come even from the same node.
        /// </remarks>
        public float GetUCT(int totalPlayout, GameInfo root_gameInfo){
            float c = 1.414f;
            // I don't know what should be UCT when no playout has happened yet
            // here it returns random value that is definitetly larger than the UCT in normal case(below)
            if(PlayoutCount == 0){
                Random r = new Random();
                return c*totalPlayout*totalPlayout + (float)r.NextDouble();
            }else{
                /*
                if(GameInfo.IsSamePlayer(this.GameInfo, root_gameInfo)){
                    float reword = WinCount / PlayoutCount;
                    float bias = (float)(c * Math.Pow(Math.Log(totalPlayout) / PlayoutCount, 0.5));
                    return reword + bias;
                }
                else{
                    float reword = (PlayoutCount - WinCount) / PlayoutCount; // reverse player side
                    float bias = (float)(c * Math.Pow(Math.Log(totalPlayout) / PlayoutCount, 0.5));
                    return reword + bias;
                }
                */
                float reword = WinCount / PlayoutCount;
                float bias = (float)(c * Math.Pow(Math.Log(totalPlayout) / PlayoutCount, 0.5));
                return reword + bias;
            }
        }

        public float GetWinRate(){
            if(PlayoutCount == 0){
                return 0;
            }else{
                return WinCount / PlayoutCount;
            }
        }

        public void PrintNode(string title = ""){
            if(!string.IsNullOrEmpty(title)){
                Console.WriteLine(title);
            }

            Console.WriteLine("PlayoutCount: " + PlayoutCount);
            Console.WriteLine("WinCount: " + WinCount);
            Console.WriteLine("Win Rate: " + WinCount / PlayoutCount);
            GameInfo.PrintGameInfo("<== GameInfo ==>");
        }
    }

    public class GameInfo{
        public Piece Player{ set; get; }
        public int Board_size_X{ private set; get; } // must be 2 or more
        public int Board_size_Y{ private set; get; } // must be 2 or more
        public string BoardString{ set; get; }
        public Move PreMove{ set; get; } // previous move

        public GameInfo(Piece player, int board_size_X, int board_size_Y, string boardString, Move preMove){
            Player = player;
            Board_size_X = board_size_X;
            Board_size_Y = board_size_Y;
            BoardString = boardString;
            PreMove = preMove;
        }

        public bool IsExpandable(){
            TicTacToe ttt = new TicTacToe(Board_size_X, Board_size_Y);
            ttt.SetBoard(BoardString);
            return ttt.GetWinner() == null;
        }

        public float Playout(){
            TicTacToe ttt = new TicTacToe(Board_size_X, Board_size_Y);
            ttt.SetBoard(BoardString);
            return ttt.Playout(Player);
        }

        public void PrintGameInfo(string title = ""){
            if(!string.IsNullOrEmpty(title)){
                Console.WriteLine(title);
            }

            TicTacToe.PrintBoard(BoardString, Board_size_X, Board_size_Y);

            Console.WriteLine("Player: " + Player);
            Console.WriteLine("Board_size_X: " + Board_size_X);
            Console.WriteLine("Board_size_Y: " + Board_size_Y);
        }

        public List<Node> GetPossibleChildren(){
            TicTacToe ttt = new TicTacToe(Board_size_X, Board_size_Y);
            var nextBoardInfos = ttt.GetAllNextBoards(ttt.String2Board(BoardString), Player);

            List<Node> children = new List<Node>();

            foreach(var nextBoardInfo in nextBoardInfos){
                GameInfo childGameInfo = new GameInfo(
                    TicTacToe.GetNextPlayer(Player),
                    Board_size_X,
                    Board_size_Y,
                    nextBoardInfo.boardString,
                    new Move(
                        nextBoardInfo.x,
                        nextBoardInfo.y,
                        nextBoardInfo.piece
                    )
                );
                Node child = new Node(childGameInfo);
                children.Add(child);
            }

            return children;
        }

        public static bool IsSamePlayer(GameInfo gameInfo1, GameInfo gameInfo2){
            if(gameInfo1.Player == gameInfo2.Player) return true;
            else return false;
        }
    }
    
}
