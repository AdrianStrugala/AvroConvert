# SolTechnology Commercial Licence for AvroConvert

Version 1.0 — applies to AvroConvert 4.0.0 and later.

Licensor: Adrian Strugała Sol Technology, a sole proprietorship registered in Poland (CEIDG),
Młodych Techników 4/38, 53-646 Wrocław, Poland, NIP 8971889807, trading as SolTechnology
("SolTechnology").
Licensee: the organisation named on the purchase invoice ("Customer").
Software: the AvroConvert library in version 4.0 and later **and the 3.4.x maintenance releases**, its
satellite packages published by SolTechnology (SolTechnology.Avro.Http, SolTechnology.Avro.Kafka, SolTechnology.Avro.Cli) and
their documentation. The 3.4.x releases are otherwise published under CC BY-NC-SA 3.0; this Licence
grants the Customer the additional right to use them commercially, so that an organisation that
cannot yet move to .NET 10 can be licensed today and upgrade when ready.

## 1. Grant

Subject to payment of the applicable fee, SolTechnology grants the Customer a worldwide,
non-exclusive, non-transferable licence to use, copy, modify and embed the Software in the Customer's
own products and services, for any purpose, during the Subscription Term.

## 2. Scope and tiers

- The licence covers the **whole organisation** named on the invoice, including its wholly owned
  subsidiaries, with an unlimited number of developers, applications, servers and end users.
- The Commercial Licence is sold in two tiers. The tier is determined by the size and status of the
  Customer, not by the Customer's choice:
  - **Business** is available only to a Customer that, together with its affiliates, (a) has **fewer
    than 1,000 total individuals** working as employees and independent contractors, (b) has no
    securities listed on a public stock exchange, and (c) is not a public-sector body (a government,
    governmental agency, public university or other entity controlled by a public authority).
  - **Enterprise** is **required** for every Customer that does not meet all three conditions above,
    and may be purchased by any Customer. Enterprise additionally covers the Customer's affiliates
    that are not wholly owned subsidiaries, and includes the support described in the price list at
    https://soltechnology.dev/avroconvert/.
  - "Affiliate" means any entity that controls, is controlled by, or is under common control with
    the Customer. Headcount is measured on the purchase or renewal date.
- A Business Customer that ceases to meet the Business conditions during the Subscription Term may
  continue under its Business licence until the end of that term, and must purchase Enterprise at the
  next renewal. Use under a Business licence by a Customer that did not qualify for it on the
  purchase date is unlicensed.
- The Customer may distribute the Software only as part of its own products or services, in binary
  form, and may not offer it as a stand-alone library or as a competing serialisation product.

## 3. Subscription Term and perpetual fallback

- The Subscription Term is **12 months** from the purchase date and renews only when the Customer
  pays for a further term.
- After the term ends, the Customer keeps a **perpetual** licence to every version of the Software
  released **before** the end of the term, under these same conditions. Versions released after that
  date require an active subscription.

## 4. Updates and support

During the Subscription Term the Customer is entitled to all releases of the Software and to the
support channel of the purchased tier. Support is provided in English, on business days, on a
best-effort basis unless the tier specifies response times.

## 5. Fees, taxes and refunds

Fees are collected by the merchant of record (Polar Software, Inc. or its affiliates), who issues
the invoice and handles applicable taxes; purchase orders and bank transfers are invoiced directly by
SolTechnology. Refunds are available within 14 days of purchase if the
Software has not been deployed to production — see https://soltechnology.dev/refunds.

## 6. Ownership

The Software is licensed, not sold. SolTechnology and its licensors retain all rights not expressly
granted. Parts of the Software derived from Apache Avro remain under the Apache License 2.0.

## 7. Warranty disclaimer

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
LIMITED TO WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT.

## 8. Limitation of liability

TO THE MAXIMUM EXTENT PERMITTED BY LAW, SOLTECHNOLOGY'S TOTAL LIABILITY UNDER THIS LICENCE IS LIMITED
TO THE FEES PAID BY THE CUSTOMER IN THE 12 MONTHS PRECEDING THE CLAIM, AND SOLTECHNOLOGY IS NOT LIABLE
FOR ANY INDIRECT, INCIDENTAL, SPECIAL OR CONSEQUENTIAL DAMAGES OR LOSS OF DATA OR PROFITS.

## 9. Termination

SolTechnology may terminate this licence if the Customer materially breaches it and does not cure
the breach within 30 days of written notice. On termination for breach the Customer must stop using
the Software; the perpetual fallback in section 3 does not survive termination for breach.

## 10. Governing law

This licence is governed by the laws of Poland. Disputes are subject to the courts competent for the
seat of SolTechnology, without prejudice to mandatory consumer protection rules where they apply.

## 11. Licence key

After purchase the Customer receives by e-mail a licence certificate and a licence key naming the
Customer, the tier and the Subscription Term. The Customer should declare it in its code base
(`AvroConvert.License = AvroLicense.Commercial("<licence key>")`). The key is for transparency and
audits only; the Software never restricts functionality based on it. The Customer may share the key
within its organisation but not with third parties.

## 12. Licensees of AvroConvert 3.x

Commercial licences for AvroConvert 3.x and earlier were sold by Xabe (xabe.net) under Xabe's terms.
Those licences are not affected by this Licence and continue to cover the 3.x versions as originally
agreed; the 3.4.x maintenance branch keeps receiving security fixes.

A holder of such a licence is entitled, **without time limit and free of charge**, to a licence under
this Licence for version 4.0 **and every later version**, in the tier that applies to the Customer under
section 2. To receive the licence key and certificate, the Customer sends proof of purchase (the Xabe
invoice or licence document) to contact@soltechnology.dev. The licence so granted is **perpetual**: it
has no Subscription Term, does not require renewal, and covers all future releases of the Software.
All other provisions of this Licence apply.
