# ZenHanConverter ユーザー利用仕様書

## 概要
- 全角・半角・カタカナ/ひらがな・記号などの相互変換を提供するユーティリティです。
- 変換定義は CSV からコード生成され、`ConvertPairs`/`EntryRecord` オブジェクトとして高速に扱えます。
- 入力を正規化してから変換する複合メソッド (`ToHan`, `ToZenWithKatakana` など) を備え、アプリケーションの文字幅統一やデータクレンジングに利用できます。

## 対応環境
- .NET 8 以上（ライブラリ本体）

## リリースビルド での ベンチマーク結果
以下は、1000文字程度の混合テキスト（英数字・仮名・記号）を 10,000 回ループ処理した際の実行時間計測結果です。（.NET 8 Environment）

| メソッド | 実行時間 (10,000回合計) | 1回あたりの平均 | 処理速度 (約) |
| --- | --- | --- | --- |
| `ToNormalize` | 131 ms | 0.013 ms | 13.1 µs |
| `ToHan` | 1585 ms | 0.159 ms | 158.5 µs |
| `ToZenWithKatakana` | 179 ms | 0.018 ms | 17.9 µs |

※ `ToHan` は変換対象のパターンが多く (`Ascii` + `Kana` 全域)、正規表現のマッチング負荷が高いため相対的に時間を要しますが、それでも 10k 文字/ms 級 (6MB/s程度) のスループットが出ています。
※ `ToNomalize` や `ToZen` 系は非常に高速です。

## 変換できる文字とカテゴリ
- 数字: `０`～`９` ↔ `0`～`9`
- 英字: 全角/半角の大文字・小文字 `Ａ`～`Ｚ`、`ａ`～`ｚ` ↔ `A`～`Z`、`a`～`z`
- 記号: 括弧類、クォート類、区切り記号、算術/比較記号、`＼`/`\\`/`￥`/`¥`/スペース など
- カタカナ/ひらがな: 清音・濁音・半濁音・小書き文字を全角/半角で相互変換。`゛`/`゜` 長音 `ー`、中点 `・`、句読点 `、` `。` も対応
- ケース変換: 全角・半角それぞれの大文字⇔小文字
- カナの結合: 分離した文字 (`カ`+`゛` 等) を合成/分離 (`KataCompose`/`HiraCompose`)
- 特殊ケース: 細かな空白（ノーブレークスペース等）を通常スペースへ、各種ダッシュをハイフンへ正規化

## 主な API 詳説
| メソッド | 内容 |
| --- | --- |
| string `ToNormalize(string text)` | 空白・ダッシュ・分離カナを正規化 |
| string `ToHan(string text)` | 数字/英字/記号/カナ（長音/濁点含む）を半角へ統一 |
| string `ToZenWithKatakana(string text)` | 数字/英字/記号/半角カナを全角カタカナへ統一 |
| string `ToZenWithHiragana(string text)` | 数字/英字/記号/半角カナを全角ひらがなへ統一 |
| string `ToHanOnlyAscii(string text)` | 数字・英字・記号のみ半角化し、カナは変更しない |
| string `ToZenOnlyAscii(string text)` | 数字・英字・記号のみ全角化 |
| string `ToHanOnlyKana(string text)` | ひらがな/カタカナと関連記号を半角化 |
| string `ToHanOnlyKatakana(string text)` | カタカナだけ半角化 |
| string `ToZenKatakanaOnlyKatakana(string text)` | 半角カタカナを全角カタカナに統一 |
| string `ToZenKatakanaOnlyKana(string text)` | ひらがな+半角カナを全角カタカナへ |
| string `ToZenHiraganaOnlyKana(string text)` | カタカナ（全角/半角）をひらがなへ |
| string `ToUpperCase(string text)` | 全角/半角英字を大文字へ |
| string `ToLowerCase(string text)` | 全角/半角英字を小文字へ |
| string `ConvertTabToSpace(string text)` | タブを半角スペースへ |
| string `ConvertBackslashToHanYen(string text)` | バックスラッシュを半角円記号へ |

### 使い方サンプル
```csharp
using EsUtil.Helper;

// 正規化（空白/ダッシュ/分離カナ）
var normalized = ZenHanConverter.ToNormalize("ｶﾞ  ｶﾞｰ"); // "ガ ガ-"

// 全カテゴリを半角へ
var han = ZenHanConverter.ToHan("ＡＢＣ１２３　カナ。"); // "ABC123 ｶﾅ｡"

// 全角カタカナで統一
var zenKata = ZenHanConverter.ToZenWithKatakana("ABC123 ｶﾀｶﾅ かな"); // "ＡＢＣ１２３ カタカナ カナ"

// 全角ひらがなで統一
var zenHira = ZenHanConverter.ToZenWithHiragana("ABC123 ｶﾀｶﾅ カナ"); // "ＡＢＣ１２３ かたかな かな"

// 数字・英字・記号のみ半角化
var hanAscii = ZenHanConverter.ToHanOnlyAscii("ＡＢＣ１２３ カナ"); // "ABC123 カナ"

// 数字・英字・記号のみ全角化
var zenAscii = ZenHanConverter.ToZenOnlyAscii("ABC123 カナ"); // "ＡＢＣ１２３ カナ"

// かな関連のみ半角化
var hanKana = ZenHanConverter.ToHanOnlyKana("カナかな。ー"); // "ｶﾅかな｡-"

// カタカナのみ半角化
var hanKata = ZenHanConverter.ToHanOnlyKatakana("カナかな。ー"); // "ｶﾅかな。ー"

// 半角カタカナを全角カタカナへ
var zenKataOnlyKata = ZenHanConverter.ToZenKatakanaOnlyKatakana("カナかな ｶﾀｶﾅ"); // "カナかな カタカナ"

// ひらがな+半角カナを全角カタカナへ
var zenKataOnlyKana = ZenHanConverter.ToZenKatakanaOnlyKana("かな ｶﾅ"); // "カナ カナ"

// カタカナ（全/半）をひらがなへ
var zenHiraOnlyKana = ZenHanConverter.ToZenHiraganaOnlyKana("カナ ｶﾅ"); // "かな かな"

// 英字を大文字へ（全角/半角対応）
var upper = ZenHanConverter.ToUpperCase("abc ａｂｃ"); // "ABC ＡＢＣ"

// 英字を小文字へ（全角/半角対応）
var lower = ZenHanConverter.ToLowerCase("ABC ＡＢＣ"); // "abc ａｂｃ"

// タブを半角スペースへ
var tabs = ZenHanConverter.ConvertTabToSpace("\tABC\t"); // " ABC "

// バックスラッシュを半角円記号へ
var yen = ZenHanConverter.ConvertBackslashToHanYen(@"\\"); // "¥"

// ConvertPairs で任意の組合せを構築
var pairs = new ZenHanConverter.ConvertPairs([( "◎", "○" ), ( "○", "◯" )]);
var chained = pairs.ChainMerge(new ZenHanConverter.ConvertPairs([( "◯", "●" )]));
var result = chained.Convert("◎○◯"); // "○◯●"
```

## 変換定義の直接利用
- 主な API では対応しない細かな変換を行う場合、`GroupOf` クラス経由で定義済みの変換ペア (`ConvertPairs`) を取得できます。

### 定義グループ (`GroupOf`)
`GroupOf` 静的クラス以下にカテゴリごとに分類されたプロパティが定義されています。

| グループ (プロパティ) | 内容 |
| --- | --- |
| `GroupOf.Ascii` | 英数字・記号関連の変換定義の集合 |
| `GroupOf.Kana` | かな（ひらがな・カタカナ）関連の変換定義の集合 |

これらを通して、さらに細かい単位のマップ (`IEnumerable<(string Source, string Target)>`) にアクセス可能です。
例: `GroupOf.Ascii.ToHanMap`, `GroupOf.Kana.ToZenMap` など。

### `ConvertPairs` の使い方と変換方法
| メソッド | 内容 |
| --- | --- |
| string `Convert(string text)` | 置換 |
| ConvertPairs `Chain(ConvertPairs second, bool, bol)` | 変換先一致で連結し、未マッチの保持可否を制御 |
| ConvertPairs `Chain(ConvertPairs second)` | マッチ部分のみ連鎖 |
| ConvertPairs `ChainMerge(ConvertPairs second)` | 未マッチも含めて結合 |

## 定義済みエントリ一覧 (GroupOf / NameOf)
`GroupOf` クラスおよび `NameOf` クラスでアクセス可能な定義一覧です。
`GroupOf.{カテゴリ}.{グループ}` で `ConvertPairs` を、`NameOf.{カテゴリ}.{定義名}` で個別の定義を取得できます。

| カテゴリ (`GroupOf`) | グループ (`GroupOf`プロパティ) / 定義名 (`NameOf`プロパティ) |
| --- | --- |
| `Ascii`<br/>(英数字・記号) | ***`Numeric`*** (数字 グループ)<br/>`n0`: ０<br/>`n1`: １<br/>`n2`: ２<br/>`n3`: ３<br/>`n4`: ４<br/>`n5`: ５<br/>`n6`: ６<br/>`n7`: ７<br/>`n8`: ８<br/>`n9`: ９ |
| `Ascii`<br/>(英数字・記号) | ***`Alphabet`*** (英字 グループ)<br/>※全角/半角、大文字/小文字の全パターンを網羅<br/>`A`: Ａ<br/>`B`: Ｂ<br/>`C`: Ｃ<br/>`D`: Ｄ<br/>`E`: Ｅ<br/>`F`: Ｆ<br/>`G`: Ｇ<br/>`H`: Ｈ<br/>`I`: Ｉ<br/>`J`: Ｊ<br/>`K`: Ｋ<br/>`L`: Ｌ<br/>`M`: Ｍ<br/>`N`: Ｎ<br/>`O`: Ｏ<br/>`P`: Ｐ<br/>`Q`: Ｑ<br/>`R`: Ｒ<br/>`S`: Ｓ<br/>`T`: Ｔ<br/>`U`: Ｕ<br/>`V`: Ｖ<br/>`W`: Ｗ<br/>`X`: Ｘ<br/>`Y`: Ｙ<br/>`Z`: Ｚ |
| `Ascii`<br/>(英数字・記号) | ***`Symbol`*** (記号 グループ)<br/>`ParenthesisLeft`: 丸括弧 左<br/>`ParenthesisRight`: 丸括弧 右<br/>`SquareBracketLeft`: 角括弧 左<br/>`SquareBracketRight`: 角括弧 右<br/>`CurlyBracketLeft`: 波括弧 左<br/>`CurlyBracketRight`: 波括弧 右<br/>`DoubleQuote`: ダブルクォート<br/>`SingleQuote`: シングルクォート<br/>`Backquote`: バッククォート<br/>`Comma`: カンマ<br/>`Period`: ピリオド<br/>`Colon`: コロン<br/>`Semicolon`: セミコロン<br/>`LessThan`: 不等号 小<br/>`GreaterThan`: 不等号 大<br/>`Equal`: 等号<br/>`Plus`: プラス<br/>`HyphenMinus`: ハイフン<br/>`Question`: はてな<br/>`Exclamation`: 感嘆符<br/>`Sharp`: シャープ<br/>`Dollar`: ドル<br/>`Percent`: パーセント<br/>`Ampersand`: アンパサンド<br/>`Asterisk`: アスタリスク<br/>`At`: アットマーク<br/>`Caret`: キャレット<br/>`UnderBar`: アンダーバー<br/>`VerticalBar`: 縦棒<br/>`Tilde`: チルダ<br/>`Slash`: スラッシュ<br/>`Bslash`: 逆斜線 (U+005C)<br/>`Yen`: 円 (U+00A5)<br/>`Space`: スペース (U+3000 / U+0020) |
| `Ascii`<br/>(英数字・記号) | ***`Replace`*** (置換・正規化 グループ)<br/>`Space`: 各種特殊空白を U+0020 へ<br/>`Hyphen`: 各種ダッシュ/マイナスを U+002D へ<br/>`Tab`: タブ (U+0009) を U+0020 へ<br/>`Yen`: 全角/半角円記号 ⇔ バックスラッシュ<br/>`Bslash`: バックスラッシュ ⇔ 円記号 |
| `Kana`<br/>(かな) | ***`Kata`*** (カタカナ グループ)<br/>※清音/濁音/半濁音/小書き、結合文字含む<br/>`A`: ア<br/>`I`: イ<br/>`U`: ウ<br/>`E`: エ<br/>`O`: オ<br/>`KA`: カ<br/>`KI`: キ<br/>`KU`: ク<br/>`KE`: ケ<br/>`KO`: コ<br/>`SA`: サ<br/>`SHI`: シ<br/>`SU`: ス<br/>`SE`: セ<br/>`SO`: ソ<br/>`TA`: タ<br/>`CHI`: チ<br/>`TSU`: ツ<br/>`TE`: テ<br/>`TO`: ト<br/>`NA`: ナ<br/>`NI`: ニ<br/>`NU`: ヌ<br/>`NE`: ネ<br/>`NO`: ノ<br/>`HA`: ハ<br/>`HI`: ヒ<br/>`FU`: フ<br/>`HE`: ヘ<br/>`HO`: ホ<br/>`MA`: マ<br/>`MI`: ミ<br/>`MU`: ム<br/>`ME`: メ<br/>`MO`: モ<br/>`YA`: ヤ<br/>`YU`: ユ<br/>`YO`: ヨ<br/>`RA`: ラ<br/>`RI`: リ<br/>`RU`: ル<br/>`RE`: レ<br/>`RO`: ロ<br/>`WA`: ワ<br/>`WO`: ヲ<br/>`N`: ン<br/>`GA`～`PO`: 濁音・半濁音 (ガ, パ 等)<br/>`VU`: ヴ |
| `Kana`<br/>(かな) | ***`Hira`*** (ひらがな グループ)<br/>※カタカナと同等のキー名でひらがな定義を提供<br/>`A`: あ<br/>`I`: い<br/>... (カタカナと同様の定義名) |
| `Kana`<br/>(かな) | ***`Symbol`*** (かな記号 グループ)<br/>`Voice`: ゛ (濁点)<br/>`SemiVoice`: ゜ (半濁点)<br/>`Prolong`: ー (長音)<br/>`MiddleDot`: ・ (中点)<br/>`LeftCornerBracket`: 「<br/>`RightCornerBracket`: 」<br/>`Period`: 。 (句点)<br/>`Comma`: 、 (読点) |
| `Kana`<br/>(かな) | ***`Case`*** (かな 大小/種別変換 グループ)<br/>※カタカナ⇔ひらがな変換用 (定義名は `Kata`/`Hira` と共通) |

## パフォーマンス向上施策
- Regex/辞書キャッシュ: `ConcurrentDictionary` でコンパイル済み Regex とマッピングをキャッシュし、再生成を回避。
- 先勝ち辞書化: 同一 Source は初出のみ採用し、決定性と無駄な上書きを防止。
- Unicode デコード先行: `EntryRecord` 生成時に `U+XXXX` を実体化し、実行時オーバーヘッドを削減。
- null/空の早期スキップ: 変換不要ケースで早期 return し、`Regex.Replace` 呼び出しを抑制。
- 不変コレクション共有: `ImmutableList` を使い副作用を排除、スレッドセーフに高速アクセス。
- 連鎖変換の辞書化: `ConvertPairs.Chain` では第2段を Source ごとにグルーピングし、連結判定を O(1) に高速化。

## ライセンス
本リポジトリの LICENSE を参照してください。
