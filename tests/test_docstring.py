# -*- coding: utf-8 -*-

"""Test doc strings support."""


def test_doc_with_ctor():
    from Python.Test import DocWithCtorTest

    assert DocWithCtorTest.__doc__ == 'DocWithCtorTest Class'
    assert DocWithCtorTest.TestMethod.__doc__ == 'DocWithCtorTest TestMethod'
    assert DocWithCtorTest.StaticTestMethod.__doc__ == 'DocWithCtorTest StaticTestMethod'


def test_doc_with_ctor_no_doc():
    from Python.Test import DocWithCtorNoDocTest

    assert DocWithCtorNoDocTest.__doc__ == 'DocWithCtorNoDocTest(x: Boolean)'
    assert DocWithCtorNoDocTest.TestMethod.__doc__ == 'TestMethod(a: Double, b: Int32) -> Void'
    assert DocWithCtorNoDocTest.StaticTestMethod.__doc__ == 'StaticTestMethod(a: Double, b: Int32) -> Void'


def test_doc_without_ctor():
    from Python.Test import DocWithoutCtorTest

    assert DocWithoutCtorTest.__doc__ == 'DocWithoutCtorTest Class'
    assert DocWithoutCtorTest.TestMethod.__doc__ == 'DocWithoutCtorTest TestMethod'
    assert DocWithoutCtorTest.StaticTestMethod.__doc__ == 'DocWithoutCtorTest StaticTestMethod'
