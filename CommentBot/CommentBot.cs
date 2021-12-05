using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TwicasAPI.v2.api;
using TwicasAPI.v2.model;

namespace CommentBot
{
    class CommentBot
    {
        /// <summary>
        /// 取得コメント数
        /// </summary>
        private const int MaxComment = 20;

        /// <summary>
        /// 待機秒数
        /// </summary>
        private const int WaitSeconds = 5 * 1000;

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("コメントBOT実行中");

                var user = new UserAPI(GetConfigPath());
                var movieId = user.GetLastMovieId();
                var api = GetCommentAPI(args);

                //起動直後はコメント保存を実施
                SaveComments(api);

                //定期的にキーワードをチェックしてコメント送信
                while (true)
                {
                    System.Threading.Thread.Sleep(WaitSeconds);
                    var comments = api.GetComments(MaxComment);
                    SendComment(api, comments, movieId);
                    api.SaveComments(comments);
                }
            }
            catch (TwicasException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.Json);
            }
        }

        /// <summary>
        /// 設定ファイルのパスを取得
        /// </summary>
        /// <returns>ファイルパス</returns>
        private static string GetConfigPath()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            return $"{path}config.json";
        }

        /// <summary>
        /// コメントAPIを取得する
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static CommentAPI GetCommentAPI(string[] args)
        {
            var comment = new CommentAPI(GetConfigPath());
            if (args.Length > 0)
            {
                if (int.TryParse(args[0], out int value))
                {
                    comment.MaxIndex = value;
                }
            }
            return comment;
        }

        /// <summary>
        /// コメントを保存する
        /// </summary>
        /// <param name="api">コメントAPI</param>
        private static void SaveComments(CommentAPI api)
        {
            var comments = api.GetComments(MaxComment);
            api.SaveComments(comments);
        }

        /// <summary>
        /// キーワードが存在するコメントがあれば、コメントを送信する
        /// </summary>
        /// <param name="api">コメントAPI</param>
        /// <param name="comments">コメント</param>
        /// <param name="movieId">ライブID</param>
        private static void SendComment(CommentAPI api, List<(string, string)> comments, string movieId)
        {
            const int millisecondsTimeout = 1000;

            //取得したコメントから処理済みコメント・除外キーワードを含むコメントを除外
            var list = comments.Except(api.GetSaveComments());
            var excludeKeyword = api.GetConfig().ExcludeKeyword;
            list = list.Where(x => !IsExist(x.Item2, excludeKeyword));

            //一致するコメントがあればコメント送信
            foreach (var item in api.GetConfig().Keyword)
            {
                var isPost = api.Contains(list, item.Key);
                if (isPost)
                {
                    foreach (var comment in item.Value)
                    {
                        var response = api.PostComment(movieId, $"{comment}{api.GetRandom()}");
                        api.SaveComments(response.Comment);
                        Thread.Sleep(millisecondsTimeout);
                    }
                }
            }
        }

        /// <summary>
        /// 存在チェック
        /// </summary>
        /// <param name="comment">コメント</param>
        /// <param name="keywords">キーワード</param>
        /// <returns>True:存在する</returns>
        private static bool IsExist(string comment, List<string> keywords)
        {
            foreach (var keyword in keywords)
            {
                if (comment.Contains(keyword))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
