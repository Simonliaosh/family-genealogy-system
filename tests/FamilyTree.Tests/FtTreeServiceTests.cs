using FamilyTree.Models;
using FamilyTree.Services;
using Xunit;

namespace FamilyTree.Tests;

/// <summary>
/// 树形逻辑的纯函数部分。这些函数决定用户在族谱页上实际看到什么，
/// 且不碰数据库，是最值得先覆盖的一块。
/// </summary>
public class FtTreeServiceTests
{
    private static FtPerson P(int id, string name, int? father = null, int? mother = null,
        string fatherName = "", string? motherName = null, bool inMain = false,
        int? birthYear = null, string? birthDate = null, int? sameAs = null) => new()
    {
        DataId = id,
        FullName = name,
        FatherPersonId = father,
        MotherPersonId = mother,
        FatherName = fatherName,
        MotherName = motherName,
        InMainGenealogy = inMain,
        BirthYear = birthYear,
        BirthDate = birthDate,
        SameAsPersonId = sameAs
    };

    // ---------- RootsFromPersons ----------

    [Fact]
    public void RootsFromPersons_ReturnsNodesWithoutFatherInSet()
    {
        var all = new List<FtPerson> { P(1, "祖"), P(2, "父", father: 1), P(3, "子", father: 2) };
        var roots = FtTreeService.RootsFromPersons(all, requireMain: false);
        Assert.Single(roots);
        Assert.Equal(1, roots[0].Id);
    }

    [Fact]
    public void RootsFromPersons_TreatsFatherNameMatchAsNonRoot()
    {
        // 父边尚未挂 ID，但父名能在集合内对上 → 不算根
        var all = new List<FtPerson> { P(1, "张大"), P(2, "张二", fatherName: "张大") };
        var roots = FtTreeService.RootsFromPersons(all, requireMain: false);
        Assert.Single(roots);
        Assert.Equal(1, roots[0].Id);
    }

    [Fact]
    public void RootsFromPersons_ExcludesSameAsRows()
    {
        var all = new List<FtPerson> { P(1, "祖"), P(2, "影子", sameAs: 1) };
        var roots = FtTreeService.RootsFromPersons(all, requireMain: false);
        Assert.Single(roots);
    }

    [Fact]
    public void RootsFromPersons_RequireMainFiltersOutPersonalRows()
    {
        var all = new List<FtPerson> { P(1, "主谱祖", inMain: true), P(2, "个人甲") };
        var roots = FtTreeService.RootsFromPersons(all, requireMain: true);
        Assert.Single(roots);
        Assert.Equal(1, roots[0].Id);
    }

    // ---------- ChildrenForTree ----------

    [Fact]
    public void ChildrenForTree_MatchesByParentId()
    {
        var parent = P(1, "父");
        var all = new List<FtPerson> { parent, P(2, "子", father: 1), P(3, "无关") };
        var kids = FtTreeService.ChildrenForTree(parent, all);
        Assert.Single(kids);
        Assert.Equal(2, kids[0].DataId);
    }

    [Fact]
    public void ChildrenForTree_MatchesByParentNameWhenIdEdgeMissing()
    {
        var parent = P(1, "张大");
        var all = new List<FtPerson> { parent, P(2, "张二", fatherName: "张大") };
        var kids = FtTreeService.ChildrenForTree(parent, all);
        Assert.Single(kids);
    }

    [Fact]
    public void ChildrenForTree_MainGenealogyOnlyRequiresIdEdgeAndMainFlag()
    {
        var parent = P(1, "父", inMain: true);
        var all = new List<FtPerson>
        {
            parent,
            P(2, "已入谱子", father: 1, inMain: true),
            P(3, "未入谱子", father: 1),                 // 有 ID 边但未入主谱
            P(4, "同名推断子", fatherName: "父", inMain: true) // 入主谱但只有姓名边
        };
        var kids = FtTreeService.ChildrenForTree(parent, all, mainGenealogyOnly: true);
        Assert.Single(kids);
        Assert.Equal(2, kids[0].DataId);
    }

    [Fact]
    public void ChildrenForTree_NeverReturnsTheParentItself()
    {
        var parent = P(1, "父", fatherName: "父");   // 自指的坏数据
        var kids = FtTreeService.ChildrenForTree(parent, new List<FtPerson> { parent });
        Assert.Empty(kids);
    }

    // ---------- DedupeSiblingByNameBirth ----------

    [Fact]
    public void DedupeSiblingByNameBirth_KeepsTheRowThatHasParentEdge()
    {
        var list = new List<FtPerson>
        {
            P(10, "张三", birthDate: "1990"),
            P(11, "张三", father: 1, birthDate: "1990")
        };
        var kept = FtTreeService.DedupeSiblingByNameBirth(list);
        Assert.Single(kept);
        Assert.Equal(11, kept[0].DataId);
    }

    [Fact]
    public void DedupeSiblingByNameBirth_KeepsDistinctBirthYears()
    {
        var list = new List<FtPerson>
        {
            P(10, "张三", birthDate: "1990"),
            P(11, "张三", birthDate: "1995")
        };
        Assert.Equal(2, FtTreeService.DedupeSiblingByNameBirth(list).Count);
    }

    [Fact]
    public void DedupeSiblingByNameBirth_ShortCircuitsOnSingleItem()
    {
        var list = new List<FtPerson> { P(1, "独子") };
        Assert.Same(list, FtTreeService.DedupeSiblingByNameBirth(list));
    }

    // ---------- OrderSiblingsByAge ----------

    [Fact]
    public void OrderSiblingsByAge_OlderFirstUnknownYearLast()
    {
        var list = new List<FtPerson>
        {
            P(1, "小弟", birthYear: 1995),
            P(2, "不详"),
            P(3, "大哥", birthYear: 1990)
        };
        var ids = FtTreeService.OrderSiblingsByAge(list).Select(x => x.DataId).ToList();
        Assert.Equal(new[] { 3, 1, 2 }, ids);
    }

    // ---------- WouldCycle（P1-13 回归） ----------

    [Fact]
    public void WouldCycle_DetectsDirectSelfParent()
    {
        var byId = new Dictionary<int, FtPerson> { [1] = P(1, "甲") };
        Assert.True(FtTreeService.WouldCycle(byId, childId: 1, parentId: 1));
    }

    [Fact]
    public void WouldCycle_DetectsMultiNodeCycle()
    {
        // 甲 的父是 乙，乙 的父是 丙；再把 甲 挂成 丙 的父就成环
        var byId = new Dictionary<int, FtPerson>
        {
            [1] = P(1, "甲", father: 2),
            [2] = P(2, "乙", father: 3),
            [3] = P(3, "丙")
        };
        Assert.True(FtTreeService.WouldCycle(byId, childId: 3, parentId: 1));
    }

    [Fact]
    public void WouldCycle_AllowsLegitimateEdge()
    {
        var byId = new Dictionary<int, FtPerson>
        {
            [1] = P(1, "甲"),
            [2] = P(2, "乙")
        };
        Assert.False(FtTreeService.WouldCycle(byId, childId: 1, parentId: 2));
    }

    [Fact]
    public void WouldCycle_FollowsMotherEdgesToo()
    {
        var byId = new Dictionary<int, FtPerson>
        {
            [1] = P(1, "甲", mother: 2),
            [2] = P(2, "乙")
        };
        Assert.True(FtTreeService.WouldCycle(byId, childId: 1, parentId: 1));
        Assert.True(FtTreeService.WouldCycle(byId, childId: 2, parentId: 1));
    }

    [Fact]
    public void WouldCycle_TerminatesOnPreExistingCycleInData()
    {
        // 库里已经有环时，遍历本身必须终止而不是死循环
        var byId = new Dictionary<int, FtPerson>
        {
            [1] = P(1, "甲", father: 2),
            [2] = P(2, "乙", father: 1)
        };
        // 3 不在环上，从 1 向上追溯会绕回 1，seen 集合负责收敛
        Assert.False(FtTreeService.WouldCycle(byId, childId: 3, parentId: 1));
        // 环内任一点都能被识别
        Assert.True(FtTreeService.WouldCycle(byId, childId: 1, parentId: 2));
    }
}
