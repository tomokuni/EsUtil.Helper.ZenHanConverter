<!-- markdownlint-disable MD041 -- バッジを先頭に配置するため -->
[![release](https://img.shields.io/github/v/release/tomokuni/EsUtil.Helper.ZenHanConverter?label=release)](https://github.com/tomokuni/EsUtil.Helper.ZenHanConverter/releases)
[![nuget](https://img.shields.io/nuget/v/EsUtil.Helper.ZenHanConverter?label=nuget)](https://www.nuget.org/packages/EsUtil.Helper.ZenHanConverter)
[![GitHub Packages](https://img.shields.io/badge/GitHub%20Packages-v2.0.0-2ea44f?logo=github)](https://github.com/tomokuni/EsUtil.Helper.ZenHanConverter/pkgs/nuget/EsUtil.Helper.ZenHanConverter)
[![build](https://github.com/tomokuni/EsUtil.Helper.ZenHanConverter/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/tomokuni/EsUtil.Helper.ZenHanConverter/actions/workflows/build.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-0078D4)](https://dotnet.microsoft.com/platform/support/policy/dotnet-core)

# ZenHanConverter ユーザー利用仕様書

## 概要

- 全角・半角・カタカナ/ひらがな・記号などの相互変換を提供するユーティリティです。
- 変換定義は CSV からコード生成され、`ConvertPairs`/`EntryRecord` オブジェクトとして高速に扱えます。
- 入力を正規化してから変換する複合メソッド (`ToHan`, `ToZenWithKatakana` など) を備え、アプリケーションの文字幅統一やデータクレンジングに利用できます。

## インストール

### NuGet.org から

```powershell
dotnet add package EsUtil.Helper.ZenHanConverter
```

- NuGet ギャラリー: <https://www.nuget.org/packages/EsUtil.Helper.ZenHanConverter>
- パッケージ名: `EsUtil.Helper.ZenHanConverter`

### GitHub Packages から

GitHub Packages は認証が必要なため、事前にソースと資格情報を登録します。

```powershell
# USERNAME は GitHub のユーザー名、TOKEN は read:packages 権限を持つ PAT を指定します
dotnet nuget add source --username USERNAME --password TOKEN --store-password-in-clear-text --name github "https://nuget.pkg.github.com/tomokuni/index.json"

dotnet add package EsUtil.Helper.ZenHanConverter
```

- GitHub Packages: <https://github.com/tomokuni/EsUtil.Helper.ZenHanConverter/pkgs/nuget/EsUtil.Helper.ZenHanConverter>
- 初回公開時の可視性は **Private** です。他のユーザーが利用する場合は、パッケージ設定から可視性を変更してください。
- GitHub Packages を併用すると、NuGet が全ソースへ問い合わせる都合で稀に `403` が出ることがあります。その場合は `nuget.config` の [Package Source Mapping](https://learn.microsoft.com/nuget/consume-packages/package-source-mapping) でソースを振り分けてください。

## 対応環境

- **.NET 10 以上**（ライブラリ本体。C# 14 で実装）
- OS に依存しないため、Windows / Linux / macOS のいずれでも動作します。
- テストはライブラリと同じ `net10.0` を対象に実行しています。

## リリースビルド での ベンチマーク結果

1000 文字の混合テキスト（英数字・全角/半角カナ・ひらがな・記号）を 10,000 回ループ処理した際の実行時間計測結果です（.NET 10 / Release / x64）。

| メソッド | 実行時間 (10,000回合計) | 1回あたりの平均 | スループット (約) |
| --- | --- | --- | --- |
| `ToNormalize` | 74 ms | 7.4 µs | 130 MB/s |
| `ToHan` | 708 ms | 70.8 µs | 13.5 MB/s |
| `ToZenWithKatakana` | 432 ms | 43.2 µs | 22.1 MB/s |

※ `ToHan` は変換対象のパターンが最も多く (`GroupOf.Ascii` + `GroupOf.Kana` の全域)、正規表現のマッチング負荷が高いため相対的に時間を要します。
※ `ToNormalize` は正規化のみのため最も高速です。

## 変換できる文字とカテゴリ

- 数字: `０`～`９` ↔ `0`～`9`
- 英字: 全角/半角の大文字・小文字 `Ａ`～`Ｚ`、`ａ`～`ｚ` ↔ `A`～`Z`、`a`～`z`
- 記号: 括弧類、クォート類、区切り記号、算術/比較記号、`＼`/`\\`/`￥`/`¥`/スペース など
- カタカナ/ひらがな: 清音・濁音・半濁音・小書き文字を全角/半角で相互変換。`゛`/`゜` 長音 `ー`、中点 `・`、句読点 `、` `。` も対応
- ケース変換: 全角・半角それぞれの大文字⇔小文字
- カナの結合: 分離した全角カナ (`カ`+`゛` 等) を合成 (`GroupOf.Kana.Kata.ComposeMap` / `GroupOf.Kana.Hira.ComposeMap`)。半角化 (`ToHan` 系) で分離した半角カナが得られます
- 特殊ケース: 細かな空白（ノーブレークスペース等）を通常スペースへ、各種ダッシュをハイフンへ正規化

## 主な API 詳説

| メソッド | 内容 |
| --- | --- |
| string `ToNormalize(string text)` | 特殊空白・各種ダッシュを正規化し、分離した全角カナを合成 |
| string `ToHan(string text)` | 数字/英字/記号/カナ（長音/濁点含む）を半角へ統一 |
| string `ToZenWithKatakana(string text)` | 数字/英字/記号/半角カナを全角へ統一（ひらがなはそのまま） |
| string `ToZenWithHiragana(string text)` | 数字/英字/記号/半角カナを全角ひらがなへ統一（全角カタカナはそのまま） |
| string `ToHanOnlyAscii(string text)` | 数字・英字・記号のみ半角化し、カナは変更しない |
| string `ToZenOnlyAscii(string text)` | 数字・英字・記号のみ全角化（半角スペースは全角スペース U+3000 へ） |
| string `ToHanOnlyKana(string text)` | ひらがな/カタカナとかな記号を半角化 |
| string `ToHanOnlyKatakana(string text)` | カタカナとかな記号のみ半角化（ひらがなはそのまま） |
| string `ToZenOnlyKatakana(string text)` | 半角カナとかな記号を全角カタカナへ（その他は変更しない） |
| string `ToZenKatakanaOnlyKana(string text)` | ひらがな+半角カナを全角カタカナへ |
| string `ToZenHiraganaOnlyKana(string text)` | カタカナ（全角/半角）をひらがなへ |
| string `ToUpperCase(string text)` | 全角/半角英字を大文字へ |
| string `ToLowerCase(string text)` | 全角/半角英字を小文字へ |
| string `ConvertTabToSpace(string text)` | タブを半角スペースへ |
| string `ConvertBackslashToHanYen(string text)` | バックスラッシュを半角円記号へ |

> `ToHan`・`ToZen` 系と `ToUpper`/`ToLowerCase` は内部で `ToNormalize` を適用します。
> `ToNormalize` のカナ合成は **全角カナ同士 / 全角+半角濁点** が対象で、半角カナ同士 (`ｶ`+`ﾞ`) は変化しません。

### 拡張メンバー（文字列に直接チェーンできる API）

上記の表のメソッドは、C# 14 の**拡張メンバー**として **文字列のインスタンス メソッドと同じ形**でも呼び出せます。
`using EsUtil.Helper.ZenHanConverter;` を記述するだけで利用できます。

| 拡張メンバー | 内容 |
| --- | --- |
| string `ToNormalize()` | 特殊空白・各種ダッシュを正規化し、分離した全角カナを合成 |
| string `ToHan()` | 数字/英字/記号/カナを半角へ統一 |
| string `ToZenWithKatakana()` | 半角カナを全角カタカナへ統一 |
| string `ToZenWithHiragana()` | 半角カナを全角ひらがなへ統一 |
| string `ToHanOnlyAscii()` / `ToZenOnlyAscii()` | 数字・英字・記号のみ半角化 / 全角化 |
| string `ToHanOnlyKana()` / `ToHanOnlyKatakana()` | かな / カタカナのみ半角化 |
| string `ToZenOnlyKatakana()` / `ToZenKatakanaOnlyKana()` | 半角カナを全角カタカナへ / かなを全角カタカナへ |
| string `ToZenHiraganaOnlyKana()` | カタカナ（全/半）をひらがなへ |
| string `ToUpperCase()` / `ToLowerCase()` | 全角/半角英字を大文字 / 小文字へ |
| string `ConvertTabToSpace()` / `ConvertBackslashToHanYen()` | タブ / バックスラッシュの置換 |
| bool `IsEmpty` | 変換ペアを 1 件も含まないか（`ConvertPairs` やペアの列挙で利用） |
| ConvertPairs `ToConvertPairs()` | ペア集合から `ConvertPairs` を生成 |

```csharp
using EsUtil.Helper.ZenHanConverter;

// 静的 API の代わりに、文字列から直接チェーンできる
var han = "ＡＢＣ１２３　カナ。".ToHan();                          // "ABC123 ｶﾅ｡"
var zen = "ABC123 ｶﾀｶﾅ かな".ToZenWithKatakana();               // "ＡＢＣ１２３　カタカナ　かな"
var normalized = "カ゛\u00A0\u2010".ToNormalize();              // "ガ -"

// ペア集合への拡張
var isEmpty = GroupOf.Ascii.ToHanMap.IsEmpty;                    // false
var pairs = new[] { ("◎", "○"), ("○", "◯") }.ToConvertPairs();
```

### 使い方サンプル

```csharp
using EsUtil.Helper.ZenHanConverter;

// 正規化（特殊空白→U+0020、各種ダッシュ→U+002D、分離した全角カナの合成）
var normalized = ZenHanConverter.ToNormalize("カ゛\u00A0\u2010"); // "ガ -"

// 半角カナは ToNormalize の合成対象外（変化しない）
var notNormalized = ZenHanConverter.ToNormalize("ｶﾞ"); // "ｶﾞ"

// 全カテゴリを半角へ
var han = ZenHanConverter.ToHan("ＡＢＣ１２３　カナ。"); // "ABC123 ｶﾅ｡"

// 全角で統一（半角カナは全角カタカナ、ひらがなはそのまま）
var zenKata = ZenHanConverter.ToZenWithKatakana("ABC123 ｶﾀｶﾅ かな"); // "ＡＢＣ１２３　カタカナ　かな"

// 半角カナを全角ひらがなへ（全角カタカナはそのまま）
var zenHira = ZenHanConverter.ToZenWithHiragana("ABC123 ｶﾀｶﾅ カナ"); // "ＡＢＣ１２３　かたかな　カナ"

// 数字・英字・記号のみ半角化
var hanAscii = ZenHanConverter.ToHanOnlyAscii("ＡＢＣ１２３ カナ"); // "ABC123 カナ"

// 数字・英字・記号のみ全角化（半角スペースも全角スペースへ）
var zenAscii = ZenHanConverter.ToZenOnlyAscii("ABC123 カナ"); // "ＡＢＣ１２３　カナ"

// ひらがな/カタカナとかな記号を半角化
var hanKana = ZenHanConverter.ToHanOnlyKana("カナかな。ー"); // "ｶﾅｶﾅ｡ｰ"

// カタカナとかな記号のみ半角化（ひらがなはそのまま）
var hanKata = ZenHanConverter.ToHanOnlyKatakana("カナかな。ー"); // "ｶﾅかな｡ｰ"

// 半角カナを全角カタカナへ
var zenKataOnlyKata = ZenHanConverter.ToZenOnlyKatakana("カナかな ｶﾀｶﾅ"); // "カナかな カタカナ"

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
var yen = ZenHanConverter.ConvertBackslashToHanYen(@"\"); // "¥"

// ConvertPairs で任意の組合せを構築
var pairs = new ConvertPairs([( "◎", "○" ), ( "○", "◯" )]);
var chained = pairs.ChainMerge(new ConvertPairs([( "◯", "●" )]));
var result = chained.Convert("◎○◯"); // "○●◯"
```

## 変換定義の直接利用

- 主な API では対応しない細かな変換を行う場合、`GroupOf` / `NameOf` クラス経由で定義済みの変換ペア (`ConvertPairs`) を取得できます。
- `GroupOf` はグループ単位、`NameOf` は定義単位でアクセスできます（詳細は「定義済みエントリ一覧」を参照）。

### 定義グループ (`GroupOf`)

`GroupOf` 静的クラス以下にカテゴリごとに分類されたプロパティが定義されています。

| グループ (プロパティ) | 内容 |
| --- | --- |
| `GroupOf.Ascii` | 英数字・記号関連の変換定義の集合 |
| `GroupOf.Kana` | かな（ひらがな・カタカナ）関連の変換定義の集合 |

これらを通して、さらに細かい単位のマップ (`IEnumerable<(string Source, string Target)>`) にアクセス可能です。
例: `GroupOf.Ascii.ToHanMap`, `GroupOf.Kana.ToZenMap`, `GroupOf.Kana.Kata.ComposeMap` など。

### `ConvertPairs` の使い方と変換方法

| メソッド | 内容 |
| --- | --- |
| string `Convert(string text)` | 置換 |
| ConvertPairs `Chain(ConvertPairs second, bool includeUnmatchedFirst, bool includeUnmatchedSecond)` | 変換先一致で連結し、未マッチの保持可否を個別に制御 |
| ConvertPairs `Chain(ConvertPairs second)` | マッチ部分のみ連鎖 |
| ConvertPairs `ChainMerge(ConvertPairs second)` | 未マッチも含めて結合 |

## 定義済みエントリ一覧 (GroupOf / NameOf)

`GroupOf` クラスおよび `NameOf` クラスでアクセス可能な定義一覧です。
`GroupOf.{カテゴリ}.{グループ}` で `ConvertPairs` を、`NameOf.{カテゴリ}.{定義名}` で個別の定義を取得できます。
いずれも `EsUtil.Helper.ZenHanConverter` 名前空間に属します。

### `GroupOf` の階層

各ノードには変換方向ごとの `ConvertPairs` プロパティ (`ToHanMap` / `ToZenMap` / `ToUpperMap` / `ToLowerMap` / `ComposeMap` / `ToAsciiMap` / `FromAsciiMap` / `ToHiraMap` / `ToKataMap` / `ToASpaceMap` / `ToAHyphenMap` / `ToSpaceFromTabMap` / `ToYenFromBslashMap` / `ToBslashFromYenMap`) が定義されています。

| パス | 内容 |
| --- | --- |
| `GroupOf.Ascii` | 英数字・記号の全定義 |
| `GroupOf.Ascii.Numeric.Number` | 数字 `０`～`９` ↔ `0`～`9` |
| `GroupOf.Ascii.Alphabet` | 英字の全定義 |
| `GroupOf.Ascii.Alphabet.Han` / `.Zen` | 半角英字 / 全角英字の大文字⇔小文字 |
| `GroupOf.Ascii.Alphabet.Large` / `.Small` | 英大文字 / 英小文字の全角⇔半角 |
| `GroupOf.Ascii.Symbol` | 記号の全定義 |
| `GroupOf.Ascii.Symbol.Bracket` / `.Fin` / `.Ope` / `.Punc` | 括弧 / 末尾記号 / 演算子 / 句読点 |
| `GroupOf.Ascii.Replace` | 置換・正規化 (`ToASpaceMap` / `ToAHyphenMap` / `ToSpaceFromTabMap` ほか) |
| `GroupOf.Ascii.Replace.Fringe` | 特殊空白・各種ダッシュの正規化 |
| `GroupOf.Ascii.Replace.Han` / `.Zen` | 半角円記号 / 全角円記号とバックスラッシュの相互変換 |
| `GroupOf.Kana` | かなの全定義 (`ToHanMap` / `ToZenMap` / `ToHiraMap` / `ToKataMap` / `ComposeMap` / `ToAsciiMap`) |
| `GroupOf.Kana.Kata` | カタカナ |
| `GroupOf.Kana.Kata.Large` / `.Small` | カタカナ 清音/濁音/半濁音 / 小書き文字 |
| `GroupOf.Kana.Kata.ZZ` / `.ZH` / `.HZ` | 分離カタカナの合成（全-全 / 全-半 / 半-全） |
| `GroupOf.Kana.Hira` | ひらがな |
| `GroupOf.Kana.Hira.Large` / `.Small` | ひらがな 清音/濁音/半濁音 / 小書き文字 |
| `GroupOf.Kana.Hira.ZZ` / `.ZH` | 分離ひらがなの合成（全-全 / 全-半） |
| `GroupOf.Kana.Symbol` | かな記号 |
| `GroupOf.Kana.Symbol.Voice` / `.Han` / `.Zen` / `.Punc` | 濁点・半濁点 / 半角かな記号 / 全角かな記号 / 句読点 |
| `GroupOf.Kana.Case` | カタカナ⇔ひらがな (`ToHiraMap` / `ToKataMap`) |
| `GroupOf.Kana.Case.Large` / `.Small` | かな 清音等 / 小書き文字 |

### `NameOf` の階層

| パス | 内容 |
| --- | --- |
| `NameOf.Ascii.n0`～`n9` | 数字 `０`～`９` |
| `NameOf.Ascii.A`～`Z` | 英字。`.Large`(英大文字の全⇔半)、`.Small`(英小文字の全⇔半)、`.Han`(半角の大⇔小)、`.Zen`(全角の大⇔小) |
| `NameOf.Ascii.ParenthesisLeft` ほか | 記号（後述の記号名一覧を参照） |
| `NameOf.Ascii.Space.Symbol` / `.Replace` | スペース |
| `NameOf.Ascii.Bslash.Symbol` / `.Replace.Han` / `.Replace.Zen` | バックスラッシュ |
| `NameOf.Ascii.Yen.Symbol` / `.Replace.Han` / `.Replace.Zen` | 円記号 |
| `NameOf.Ascii.Hyphen` / `.Tab` | 各種ダッシュ / タブ |
| `NameOf.Kana.{A, I, U, E, O, KA, KI, ..., N, GA, ..., VU}` | かな。`.Kata`(カタカナ)、`.Hira`(ひらがな)、`.Case`(カタカナ⇔ひらがな) |
| `NameOf.Kana.Voice` / `.SemiVoice` | 濁点 `゛` / 半濁点 `゜` |
| `NameOf.Kana.MiddleDot` | 中点 `・` |
| `NameOf.Kana.Prolong.Voice` / `.Han` / `.Zen` | 長音 `ー` / `ｰ` |
| `NameOf.Kana.LeftCornerBracket` / `.RightCornerBracket` | かぎ括弧 `「` / `」` |
| `NameOf.Kana.Period.Punc` / `.Han` / `.Zen` | 句点 `。` / `｡` |
| `NameOf.Kana.Comma.Punc` / `.Han` / `.Zen` | 読点 `、` / `､` |

#### `NameOf.Ascii` の記号名一覧

| 定義名 | 内容 |
| --- | --- |
| `ParenthesisLeft` / `ParenthesisRight` | 丸括弧 左 / 右 |
| `SquareBracketLeft` / `SquareBracketRight` | 角括弧 左 / 右 |
| `CurlyBracketLeft` / `CurlyBracketRight` | 波括弧 左 / 右 |
| `DoubleQuote` / `SingleQuote` / `Backquote` | ダブルクォート / シングルクォート / バッククォート |
| `Comma` / `Period` / `Colon` / `Semicolon` | カンマ / ピリオド / コロン / セミコロン |
| `LessThan` / `GreaterThan` / `Equal` | 不等号 小 / 不等号 大 / 等号 |
| `Plus` / `HyphenMinus` / `Tilde` / `Slash` | プラス / ハイフン / チルダ / スラッシュ |
| `Question` / `Exclamation` | はてな / 感嘆符 |
| `Sharp` / `Dollar` / `Percent` | シャープ / ドル / パーセント |
| `Ampersand` / `Asterisk` / `At` | アンパサンド / アスタリスク / アットマーク |
| `Caret` / `UnderBar` / `VerticalBar` | キャレット / アンダーバー / 縦棒 |

## パフォーマンス向上施策

- Regex/辞書キャッシュ: `ConcurrentDictionary` でコンパイル済み Regex とマッピングをキャッシュし、再生成を回避。
- 先勝ち辞書化: 同一 Source は初出のみ採用し、決定性と無駄な上書きを防止。
- Unicode デコード先行: `EntryRecord` 生成時に `U+XXXX` を実体化し、実行時オーバーヘッドを削減。
- null/空の早期スキップ: 変換不要ケースで早期 return し、`Regex.Replace` 呼び出しを抑制。
- 不変コレクション共有: `ImmutableList` を使い副作用を排除、スレッドセーフに高速アクセス。
- 連鎖変換の辞書化: `ConvertPairs.Chain` では第2段を Source ごとにグルーピングし、連結判定を O(1) に高速化。

## ライセンス

本リポジトリの LICENSE を参照してください。
