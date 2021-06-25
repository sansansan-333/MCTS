using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

public enum Piece{
    O,
    X,
    N // null, not set to the cell
}
public class TicTacToe{
    private int board_size_X; // must be 2 or more
    private int board_size_Y; // must be 2 or more
    public Piece[,] Board{ private set; get; }
    public string BoardString{
        private set{}
        get {return Board2String(Board);}
    }

    public static readonly Piece[] playerOrder = {
        // Represent the order of players 
        Piece.O,
        Piece.X,
    };

    public TicTacToe(int board_size_X, int board_size_Y){
        if(board_size_X < 2 || board_size_Y < 2){
            throw new ArgumentException("Board size must be 2 or more.");
        }

        this.board_size_X = board_size_X;
        this.board_size_Y = board_size_Y;

        Board = new Piece[board_size_X, board_size_Y];
        for(int x = 0; x < board_size_X; x++){
            for(int y = 0; y < board_size_Y; y++){
                Board[x, y] = Piece.N;
            }
        }
    }

    /// <summary>
    /// Copy constructor
    /// </summary>
    /// <param name="ttt"></param>
    public TicTacToe(TicTacToe ttt){
        board_size_X = ttt.board_size_X;
        board_size_Y = ttt.board_size_Y;
        Board = new Piece[ttt.Board.GetLength(0),ttt.Board.GetLength(1)];
        Array.Copy(ttt.Board, Board, ttt.Board.Length);
    }

    public bool PlacePiece(int x, int y, Piece piece){
        if(x < 0 || y < 0 || x >= board_size_X || y >= board_size_Y){
            Console.Error.WriteLine(nameof(PlacePiece) + ": Input coordinate is invalid.");
            return false;
        }

        if(Board[x, y] == Piece.N){
            Board[x, y] = piece;
            return true;
        }else{
            return false;
        }
    }

    public bool SetBoard(Piece[,] board){
        if(board.GetLength(0) == 0 || board.GetLength(1) == 0 || 
           board.GetLength(1) > board_size_Y || board.GetLength(0) > board_size_X){
               return false;
        }

        this.Board = board;
        return true;
    }

    public bool SetBoard(string boardString){
        var board = String2Board(boardString);
        return SetBoard(board);
    }

    ///<summary>
    /// Return winner's pieces type.
    ///</summary>
    ///<returns>
    ///null or winner if there is winner
    ///</returns>
    public Piece? GetWinner(){
        // Diagonal
        if(board_size_X == board_size_Y){
            if(this.IsLineCompleted(0,0, board_size_X-1,board_size_Y-1)) return Board[0,0];
            if(this.IsLineCompleted(board_size_X-1,0, 0,board_size_Y-1)) return Board[board_size_X-1,0];
        }

        // Vertical
        for(int x = 0; x < board_size_X; x++){
            if(this.IsLineCompleted(x,0, x,board_size_Y-1)) return Board[x,0];
        }

        // Horizontal
        for(int y = 0; y < board_size_Y; y++){
            if(this.IsLineCompleted(0,y, board_size_X-1,y)) return Board[0,y];
        }
        
        return null;
    }

    ///<summary>
    /// Check if the specified line is full of one type of piece except Piece.N.
    ///</summary>
    private bool IsLineCompleted(int p1_x, int p1_y, int p2_x, int p2_y){
        int diff_x = p2_x - p1_x;
        int diff_y = p2_y - p1_y;

        // return if specified line is not straight, like p1(0, 0) to p2(2, 3)
        if( !(
            (diff_x == 0 && diff_y != 0) || 
            (diff_x != 0 && diff_y == 0) ||
            (Abs(diff_x) == Abs(diff_y) && diff_x*diff_y != 0) 
            )
            ){ 
                System.Console.WriteLine(nameof(IsLineCompleted) + ": The arguments are wrong.");
                return false;
        }

        int x = p1_x, y = p1_y;
        int next_x, next_y;
        int direction_x = diff_x == 0 ? 0 : (int)(diff_x/Abs(diff_x));
        int direction_y = diff_y == 0 ? 0 : (int)(diff_y/Abs(diff_y));

        Piece first_piece = Board[x, y];
        if(first_piece == Piece.N) return false;

        while(x != p2_x || y != p2_y){
            next_x = x + direction_x;
            next_y = y + direction_y;
            if(first_piece != Board[next_x, next_y]){
                return false;
            }
            x = next_x;
            y = next_y;
        }

        return true;
    }

    /// <summary>
    /// Check if the board is full of pieces
    /// </summary>
    /// <returns></returns>
    public bool IsFullBoard(){
        for(int x = 0; x < board_size_X; x++){
            for(int y = 0; y < board_size_Y; y++){
                if(Board[x, y] == Piece.N) return false;
            }
        }
        return true;
    }

    public void PrintBoard(string title=""){
        // Because x and y of "board" are reversed in this class,
        // there is a reverse in this function so that the printed board can be recognizable for you
        if(!string.IsNullOrEmpty(title)) Console.WriteLine(title);
        for(int x = 0; x < board_size_Y; x++){
            for(int y = 0; y < board_size_X; y++){
                Console.Write("|");
                Console.Write(Board[y, x] == Piece.N ? " " : Board[y, x].ToString());
            }
            Console.WriteLine("|");
        }
    }

    public static void PrintBoard(Piece[,] board, int board_size_X, int board_size_Y, string title=""){
        if(!string.IsNullOrEmpty(title)) Console.WriteLine(title);
        for(int x = 0; x < board_size_Y; x++){
            for(int y = 0; y < board_size_X; y++){
                Console.Write("|");
                Console.Write(board[y, x] == Piece.N ? " " : board[y, x].ToString());
            }
            Console.WriteLine("|");
        }
    }

    public static void PrintBoard(string boardString, int board_size_X, int board_size_Y, string title=""){
        var board = TicTacToe.String2Board(boardString, board_size_X, board_size_Y);
        TicTacToe.PrintBoard(board, board_size_X, board_size_Y, title);
    }

    /// <summary>
    /// Convert a board to a string like "OOXXOONNN".
    /// </summary>
    /// <param name="board"></param>
    /// <returns></returns>
    public string Board2String(Piece[,] board){
        StringBuilder result = new StringBuilder();

        for(int x = 0; x < board_size_X; x++){
            for(int y = 0; y < board_size_Y; y++){
                result.Append(board[x,y].ToString());
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Convert a string like "OOXXOONNN" to a board.
    /// </summary>
    /// <param name="boardString"></param>
    /// <returns></returns>
    public Piece[,] String2Board(string boardString){
        Piece[,] result = new Piece[board_size_X, board_size_Y];
        int str_i=0;

        for(int x = 0; x < board_size_X; x++){
            for(int y = 0; y < board_size_Y; y++){
                Piece? p = String2Piece(boardString[str_i].ToString());
                if(p == null){
                    Console.Error.WriteLine(nameof(String2Board) + ": boardString has an invalid value.");
                    return null;
                }else{
                    result[x,y] = (Piece)p;
                    str_i++;
                }
            }
        }

        return result;
    }

    public static Piece[,] String2Board(string boardString, int board_size_X, int board_size_Y){
        Piece[,] result = new Piece[board_size_X, board_size_Y];
        int str_i=0;

        for(int x = 0; x < board_size_X; x++){
            for(int y = 0; y < board_size_Y; y++){
                Piece? p = String2Piece(boardString[str_i].ToString());
                if(p == null){
                    Console.Error.WriteLine(nameof(String2Board) + ": boardString has an invalid value.");
                    return null;
                }else{
                    result[x,y] = (Piece)p;
                    str_i++;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Convert a sting to Piece, such as "O" to Piece.O.
    /// </summary>
    /// <param name="pieceString"></param>
    /// <returns></returns>
    private static Piece? String2Piece(String pieceString){
        foreach(Piece p in Enum.GetValues(typeof(Piece))){
            if(p.ToString() == pieceString){
                return p;
            }
        }

        Console.Error.WriteLine(nameof(String2Piece) + ": No matching item in enum Piece.");
        return null;
    }

    /// <summary>
    /// Return all possible boards following current board state
    /// </summary>
    /// <param name="board"></param>
    /// <param name="nextPlayer"></param>
    /// <returns></returns>

    public List<TTTInfo> GetAllNextBoards(Piece[,] board, Piece nextPlayer){
        List<TTTInfo> tttInfos = new List<TTTInfo>();

        TTTInfo info;
        for(int x = 0; x < board_size_X; x++){
            for(int y = 0; y < board_size_Y; y++){
                if(board[x,y] == Piece.N){
                    var tmpBoard = (Piece[,])board.Clone();
                    tmpBoard[x,y] = nextPlayer;

                    info = new TTTInfo(
                        x,
                        y,
                        nextPlayer,
                        Board2String(tmpBoard)
                    );
                    tttInfos.Add(info);
                }
            }
        }

        return tttInfos;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="next_player"></param>
    /// <returns>1: next_player wins, 0.5: draw, 0: lose</returns>
    public float Playout(Piece next_player){
        Random r = new Random();

        // If there is already a winner, return result
        if(GetWinner() != null){
            if(GetWinner() == next_player) return 1; // next_player wins(this condition should not be taken in MCTS however)
            else return 0; // next_player loses
        }

        // Find all empty cells
        List<Vector2> pointsToPlace = new List<Vector2>(); // list of empty cells
        for(int x = 0; x < board_size_X; x++){
            for(int y = 0; y < board_size_Y; y++){
                if(Board[x,y] == Piece.N){
                    pointsToPlace.Add(
                        new Vector2(x, y)
                    );
                }
            }
        }
        // Shuffle
        pointsToPlace = pointsToPlace.OrderBy(x => r.Next(pointsToPlace.Count)).ToList();

        // Copy this
        TicTacToe ttt = new TicTacToe(this);

        // Playout(random)
        Piece? winner = null;
        Piece tmp_player = next_player;
        foreach(var p in pointsToPlace){
            ttt.PlacePiece(p.x, p.y, tmp_player);

            winner = ttt.GetWinner();
            if(winner != null){
                break;
            }

            tmp_player = TicTacToe.GetNextPlayer(tmp_player);
        }

        // Judge playout result
        if(winner == null){
            return 0.5f; // draw
        }else if(winner == next_player){
            return 1; // win
        }else{
            return 0; // lose
        }
    }

    public static Piece GetNextPlayer(Piece player){
        for(int i = 0; i < playerOrder.Length; i++){
            if(playerOrder[i] == player){
                i++;
                i %= playerOrder.Length;
                return playerOrder[i];
            }
        }

        Console.Error.WriteLine(nameof(GetNextPlayer) + ": Something wrong");
        return Piece.X;
    }

    public static string GetInitBoard(int board_size_X, int board_size_Y){
        StringBuilder board = new StringBuilder();
        for(int i = 0; i < board_size_X*board_size_Y; i++){
            board.Append(Piece.N.ToString());
        }
        return board.ToString();
    }

    private float Abs(float x){
        return Math.Abs(x);
    }

    private void Log(string str){
        Console.WriteLine(str);
    }
}

public class Vector2{
    public int x, y;

    public Vector2(int x, int y){
        this.x = x;
        this.y = y;
    }
}

public class TTTInfo{
    public int x; // where the piece was placed
    public int y; // where the piece was placed
    public Piece piece; // what piece was placed
    public string boardString; // current board
    
    public TTTInfo(int x, int y, Piece piece, string boardString){
        this.x = x;
        this.y = y;
        this.piece = piece;
        this.boardString = boardString;
    }
}