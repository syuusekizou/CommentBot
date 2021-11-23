using System;
using System.Collections.Generic;
using System.Linq;
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
                    System.Threading.Thread.Sleep(5 * 1000);
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
            //取得したコメントから処理済みのコメントを除外
            var list = comments.Except(api.GetSaveComments());

            //一致するコメントがあればコメント送信
            foreach (var item in api.GetConfig().Keyword)
            {
                var isPost = api.Contains(list, item.Key);
                if (isPost)
                {
                    item.Value.ForEach(x =>
                    {
                        var response = api.PostComment(movieId, $"{x}{api.GetRandom()}");
                        api.SaveComments(response.Comment);
                    });
                }
            }
        }
    }
}
