using System;
using System.Collections.Generic;

namespace Mcts
{
    public class Mcts
    {
        private readonly int EXPANDER_THRESHOLD = 10;
        private Node root;
        private int totalPlayout;
        private string selectionFormula;

        static void Main(string[] args)
        {
            Console.WriteLine("Main start\n");

            int board_size_X = 3;
            int board_size_Y = 3;

            TicTacToe ttt = new TicTacToe(board_size_X, board_size_Y);
            ttt.PlacePiece(0, 0, Piece.O);
            ttt.PlacePiece(1, 2, Piece.X);

            GameInfo gameInfo = new GameInfo(
                    player:Piece.O,
                    board_size_X,
                    board_size_Y,
                    ttt.Board2String(ttt.Board)
            );

            Mcts mcts = new Mcts(Node.UCT);
            mcts.SetRoot(gameInfo);


            for(int i = 0; i < 10000; i++){
                Console.WriteLine(i);
                mcts.RunAllSteps();
            }

            foreach(var child in mcts.root.Children){
                child.PrintNode("-----------");
            }

            Console.WriteLine("\nMain end");
        }

        public Mcts(string selectionFormula){
            this.selectionFormula = selectionFormula;
            totalPlayout = 0;
        }

        public void SetRoot(GameInfo gameInfo){
            root = new Node(gameInfo);
            root.SetSelectionFormula(selectionFormula);
            root.SetPossibleChildren();
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
            Node child = root;
            while(!child.IsLeaf()){
                child = child.SelectChild(totalPlayout);
            }
            target_leaf = child;
        }

        private bool Expand(){
            if(target_leaf.PlayoutCount > EXPANDER_THRESHOLD){
                if(target_leaf.SetPossibleChildren() == false){
                    return false;
                }
                target_leaf = target_leaf.SelectChild(totalPlayout);
            }
            return true;
        }

        private float Simulate(){
            totalPlayout++;
            return target_leaf.Playout();
        }

        private void BackPropagate(float playout_result){
            Node updating_node = target_leaf;
            while(!updating_node.IsRoot()){
                updating_node.UpdateInfo(playout_result);
                playout_result = 1 - playout_result;
                updating_node = updating_node.Parent;
            }
        }

        
    }

    class Node{
        public int PlayoutCount{ private set; get;}
        public float WinCount{ private set; get; }

        public Node Parent{ private set; get; }
        public List<Node> Children{ private set; get; }

        private string selectionFormula;
        static public readonly string UCT = nameof(UCT);

        // TicTacTie related start ------->
        public GameInfo GameInfo{ private set; get; }
        //  <------- TicTacTie related end

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
        /// Set the formula such as UCT that are used for selecting a child node.
        /// </summary>
        /// <param name="selectionFormula"></param>
        public void SetSelectionFormula(string selectionFormula){
            this.selectionFormula = selectionFormula;
        }
 
        /// <summary>
        /// Select a child by using selection formula.
        /// </summary>
        /// <param name="totalPlayout"></param>
        /// <returns></returns>
        public Node SelectChild(int totalPlayout){
            if(selectionFormula == null){
                selectionFormula = Parent.selectionFormula;
            }

            if(selectionFormula == UCT){
                // uctでえらぶ
                float maxUCT = float.MinValue;
                Node chosenChild = new Node();
                foreach(var child in Children){
                    float UCT = child.GetUCT(totalPlayout);
                    if(UCT > maxUCT){
                        chosenChild = child;
                        maxUCT = UCT;
                    }
                }
                return chosenChild;
            }else{
                Console.Error.WriteLine(nameof(SelectChild) + "invalid selectionFormula.");
                return null;
            }
        }

        /// <summary>
        /// Set the formula and then select a child by using selection formula.
        /// </summary>
        /// <param name="totalPlayout"></param>
        /// <param name="selectionFormula"></param>
        /// <returns></returns>
        public Node SelectChild(int totalPlayout, string selectionFormula){
            this.selectionFormula = selectionFormula;
            return SelectChild(totalPlayout);
        }

        
        /// <summary>
        /// Set all possible children to this node.
        /// </summary>
        /// <returns>false if no child node is found</returns>
        public bool SetPossibleChildren(){
            // TicTacToe related function
            TicTacToe ttt = new TicTacToe(GameInfo.Board_size_X, GameInfo.Board_size_Y);
            var nextBoardStrs = ttt.GetAllNextBoardStrs(GameInfo.BoardString, GameInfo.Player);

            if(nextBoardStrs.Count == 0){
                return false;
            }

            foreach(var nextBoard in nextBoardStrs){
                GameInfo childGameInfo = new GameInfo(
                    TicTacToe.GetNextPlayer(GameInfo.Player),
                    GameInfo.Board_size_X,
                    GameInfo.Board_size_Y,
                    nextBoard
                );
                Node child = new Node(childGameInfo);
                child.Parent = this;
                Children.Add(child);
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
        public float GetUCT(int totalPlayout){
            float c = 1.414f;
            // I don't know what should be UCT when no playout has happened yet
            // here it returns random value that is definitetly larger than the UCT in normal case(below)
            if(PlayoutCount == 0){
                Random r = new Random();
                return c*totalPlayout*totalPlayout + (float)r.NextDouble();
            }else{
                float reword = WinCount / PlayoutCount;
                float bias = (float)(c * Math.Pow(Math.Log(totalPlayout) / PlayoutCount, 0.5));
                return reword + bias;
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
        public Piece Player{ private set; get; }
        public int Board_size_X{ private set; get; } // must be 2 or more
        public int Board_size_Y{ private set; get; } // must be 2 or more
        public string BoardString{ set; get; }

        public GameInfo(Piece player, int board_size_X, int board_size_Y, string boardString){
            Player = player;
            Board_size_X = board_size_X;
            Board_size_Y = board_size_Y;
            BoardString = boardString;
        }

        public float Playout(){
            TicTacToe ttt = new TicTacToe(Board_size_X, Board_size_Y);
            ttt.SetBoard(BoardString);
            return ttt.Playout(Player, TicTacToe.GetNextPlayer(Player));
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
    }
    
}
