﻿using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;

namespace EcoCode.Shared;

/// <summary>Extensions methods for <see cref="INamedTypeSymbol"/>.</summary>
public static class NamedTypeSymbolExtensions
{
    /// <summary>Returns whether the symbol is externally public, ie declared public and contained in public types.</summary>
    /// <param name="symbol">The symbol.</param>
    /// <returns>True if the symbol is externally public, false otherwise.</returns>
    public static bool IsExternallyPublic(this INamedTypeSymbol symbol)
    {
        do
        {
            if (symbol.DeclaredAccessibility is not Accessibility.Public) return false;
            symbol = symbol.ContainingType;
        } while (symbol is not null);
        return true;
    }

    /// <summary>Returns whether a symbol has any overridable member, excluding the default ones (Equals, GetHashCode, etc).</summary>
    /// <param name="symbol">The symbol.</param>
    /// <returns>True if any member is overridable, false otherwise.</returns>
    public static bool HasAnyExternallyOverridableMember(this INamedTypeSymbol symbol)
    {
        if (symbol.TypeKind is not TypeKind.Class || symbol.IsSealed)
            return false;

        var (current, sealedMembers) = (symbol, default(HashSet<ISymbol>));
        do
        {
            if (current.SpecialType is SpecialType.System_Object) break;

            foreach (var member in current.GetMembers())
            {
                if (member.IsImplicitlyDeclared || member.DeclaredAccessibility is not Accessibility.Public and not Accessibility.Protected)
                    continue; // IsImplicitlyDeclared is for record methods, ie. Equals, GetHashCode, etc

                if (member.IsVirtual && sealedMembers?.Contains(member) != true) return true;

                // If overridden, skip base object methods, ie. Equals, GetHashCode, etc
                if (member.OverriddenSymbol() is { } overridden && overridden.ContainingType.SpecialType is not SpecialType.System_Object)
                {
                    if (member.IsSealed) // Cache the overriden member to prevent returning true when checking it in the parent
                        _ = (sealedMembers ??= new(SymbolEqualityComparer.Default)).Add(overridden);
                    else if (sealedMembers?.Contains(overridden) != true)
                        return true; // Overriden but not sealed is still overridable
                }
            }
            current = current.BaseType;
        } while (current is not null);

        return false;
    }

    /// <summary>Finds the main declaration of a partial class or record, using a heuristic.</summary>
    /// <param name="symbol">The class symbol.</param>
    /// <param name="context">The compilation context.</param>
    /// <returns>The main class or record declaration syntax.</returns>
    public static TypeDeclarationSyntax GetPartialTypeMainDeclaration(this INamedTypeSymbol symbol, CompilationAnalysisContext context)
    {
        var bestAnalysis = default(SyntaxReferenceAnalysis);
        foreach (var syntaxRef in symbol.DeclaringSyntaxReferences)
        {
            var curAnalysis = SyntaxReferenceAnalysis.AnalyzeSyntaxReference(syntaxRef, context);
            if (curAnalysis.BetterThan(bestAnalysis)) bestAnalysis = curAnalysis;
        }
        return bestAnalysis.Decl;
    }

    private readonly struct SyntaxReferenceAnalysis(
        TypeDeclarationSyntax decl, bool hasVisibility, int baseTypes, int modifiers, int constructors, int memberCount)
    {
        public TypeDeclarationSyntax Decl { get; } = decl;
        public bool HasVisibility { get; } = hasVisibility;
        public int BaseTypes { get; } = baseTypes;
        public int Modifiers { get; } = modifiers;
        public int Constructors { get; } = constructors;
        public int MemberCount { get; } = memberCount;

        public static SyntaxReferenceAnalysis AnalyzeSyntaxReference(SyntaxReference syntaxRef, CompilationAnalysisContext context)
        {
            var decl = syntaxRef.GetSyntax(context.CancellationToken) as TypeDeclarationSyntax;
            if (decl is not ClassDeclarationSyntax and not RecordDeclarationSyntax)
                return default;

            bool hasVisibility = false;
            foreach (var modifier in decl.Modifiers)
            {
                if (modifier.IsAccessibilityKind())
                {
                    hasVisibility = true;
                    break;
                }
            }

            int baseTypes = decl.BaseList?.Types.Count ?? 0;
            int modifierCount = decl.Modifiers.Count;
            int constructorCount = 0;
            int memberCount = 0;

            foreach (var member in decl.Members)
            {
                if (member is ConstructorDeclarationSyntax)
                    constructorCount++;
                else if (member is MethodDeclarationSyntax or PropertyDeclarationSyntax)
                    memberCount++;
            }
            return new SyntaxReferenceAnalysis(decl, hasVisibility, baseTypes, modifierCount, constructorCount, memberCount);
        }

        public bool BetterThan(SyntaxReferenceAnalysis other) =>
            HasVisibility != other.HasVisibility ? HasVisibility
            : BaseTypes != other.BaseTypes ? BaseTypes > other.BaseTypes
            : Modifiers != other.Modifiers ? Modifiers > other.Modifiers
            : Constructors != other.Constructors ? Constructors > other.Constructors
            : MemberCount > other.MemberCount;
    }
}